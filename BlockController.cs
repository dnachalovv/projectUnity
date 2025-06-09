using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Unity.VisualScripting;
using static BlockController;





#if UNITY_EDITOR
using UnityEditor;
#endif

public class BlockController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler
{
    [Header("Grid Settings")]
    public int row = 0;
    public int column = 0;
    public int width = 1;
    public int height = 1;
    public LayerMask emptyLayers;
    public float minPadding;
    public float minSelfWidth;

    [Header("Block Info")]
    public int blockHashId = 0;

    public enum BlockType { TextInput, FormulaInput }
    public BlockType blockType = BlockType.TextInput;

    [Header("Text Input (if selected)")]
    public string textContent;

    [Header("Formula Input (if selected)")]
    public string formulaBase = "E";
    public string exponentTop = "n";
    public string exponentBottom = "x";

    [Header("Structures")]
    public List<Transform> blockStructures = new List<Transform>();
    public string selectedStructure = "";

    private LevelController levelController;
    private Vector3 originalPosition;

    private TMP_Text textDisplay;
    private TMP_Text baseText;
    private TMP_Text topExponent;
    private TMP_Text bottomExponent;

    private Canvas canvas;

    private SpriteRenderer sr;
    private GameObject dragCopy;

    private Image background;
    private LayoutElement layoutElement;
    private MaskableGraphic[] maskableGraphics;

    private EmptyController placedZone;
    private Transform homeParent;

    private Text textGraphic;
    private Color textNormalColor;

    private Coroutine animationCoroutine;

    private bool isDraging;

#if UNITY_EDITOR
    private bool hasPendingValidationCall = false;
#endif

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.drawMode = SpriteDrawMode.Sliced;
    }

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        background = GetComponent<Image>();
        layoutElement = GetComponent<LayoutElement>();
        maskableGraphics = GetComponentsInChildren<MaskableGraphic>();

        textGraphic = GetComponentInChildren<Text>();
        textNormalColor = textGraphic.color;

        homeParent = transform.parent;

        levelController = FindFirstObjectByType<LevelController>();
        AlignToGrid();
        UpdateBlockDisplay();
        AdjustSizeToContent();
    }

    void AlignToGrid()
    {
        if (levelController == null || sr == null) return;

        float cellSize = levelController.cellSize;
        transform.position = new Vector3(column * cellSize, row * cellSize, 0f);
        sr.size = new Vector2(width * cellSize, height * cellSize);
    }

    public void UpdateBlockDisplay()
    {
        blockStructures.RemoveAll(t => t == null);

        Transform textInput = transform.Find("TextInput");
        Transform formulaInput = transform.Find("FormulaInput");

        bool isText = blockType == BlockType.TextInput;
        bool isFormula = blockType == BlockType.FormulaInput;

        if (textInput) textInput.gameObject.SetActive(isText);
        if (formulaInput) formulaInput.gameObject.SetActive(isFormula);

        if (isText && textInput)
        {
            textDisplay = textInput.GetComponentInChildren<TMP_Text>();
            if (textDisplay)
            {
                textDisplay.text = textContent;
                textDisplay.ForceMeshUpdate();
            }
        }
        if (isFormula && formulaInput)
        {
            baseText = formulaInput.Find("BaseText")?.GetComponent<TMP_Text>();
            topExponent = formulaInput.Find("TopExponent")?.GetComponent<TMP_Text>();
            bottomExponent = formulaInput.Find("BottomExponent")?.GetComponent<TMP_Text>();
            if (bottomExponent) bottomExponent.text = exponentBottom;
            if (baseText) baseText.text = formulaBase;
            if (topExponent) topExponent.text = exponentTop;

            baseText?.ForceMeshUpdate();
            topExponent?.ForceMeshUpdate();
            bottomExponent?.ForceMeshUpdate();
        }

        AdjustSizeToContent();
    }

    private void UpdateText()
    {
        if (textGraphic != null)
        {
            switch (blockType)
            {
                case BlockType.TextInput:
                    textGraphic.text = textContent;
                    break;
                case BlockType.FormulaInput:
                    textGraphic.text = $"{formulaBase}={exponentTop}/{exponentBottom}";
                    break;
            }

            FitByText();
        }
    }
    private void FitByText()
    {
        var rectTransform = transform as RectTransform;

        float textWidth = textGraphic.preferredWidth;
        float selfWidth = minSelfWidth;

        float minWidth = minPadding + textWidth + minPadding;

        Vector2 resultSize = new Vector2(Mathf.Max(selfWidth, minWidth), rectTransform.sizeDelta.y);
        rectTransform.sizeDelta = resultSize;

        var collider = GetComponent<BoxCollider2D>();
        collider.size = resultSize;
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        textGraphic = GetComponentInChildren<Text>();
        UpdateText();

        EditorUtility.SetDirty(this);

        /*if (!Application.isPlaying && !hasPendingValidationCall) // legacy
        {
            hasPendingValidationCall = true;
            levelController = FindFirstObjectByType<LevelController>();

            EditorApplication.delayCall += () =>
            {
                hasPendingValidationCall = false;
                if (this == null) return;

                AlignToGrid();
                UpdateBlockDisplay();
                AdjustSizeToContent();

                EditorUtility.SetDirty(this);
            };
        }*/
#endif
    }

    private void FitColliderToSprite()
    {
        var box = GetComponent<BoxCollider2D>();
        if (box != null && sr != null)
        {
            box.size = sr.size;
            box.offset = Vector2.zero;
        }
    }

    private void SetBlockVisible(bool visible)
    {
        if (sr != null)
            sr.enabled = visible;

        // Скрываем/показываем дочерние визуальные элементы
        Transform textInput = transform.Find("TextInput");
        Transform formulaInput = transform.Find("FormulaInput");

        bool isText = blockType == BlockType.TextInput;
        bool isFormula = blockType == BlockType.FormulaInput;

        bool hasPlacedToZone = placedZone != null;

        if (textInput) textInput.gameObject.SetActive(visible && isText);
        if (formulaInput) formulaInput.gameObject.SetActive(visible && isFormula);

        if (background)
            background.color = visible ? Color.white : Color.white.WithAlpha(0.5f);
        if (layoutElement)
            layoutElement.ignoreLayout = hasPlacedToZone;

        foreach (var maskable in maskableGraphics)
            maskable.maskable = !hasPlacedToZone;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = transform.position;

        isDraging = true;

        Canvas canvas = GetComponentInParent<Canvas>();
        if(canvas == null)
        {
            Debug.Log("BlockController: Canvas not found in parent hierarchy.");
            return;
        }

        dragCopy = Instantiate(gameObject, canvas.transform);
        dragCopy.name = name + "_dragCopy";
        DestroyImmediate(dragCopy.GetComponent<BlockController>());
        
        SpriteRenderer copySr = dragCopy.GetComponent<SpriteRenderer>();

        Collider2D copyCollider = dragCopy.GetComponent<Collider2D>();
        if (copyCollider != null)
        {
            copyCollider.enabled = false;
        }

        if (copySr != null)
        {
            copySr.sortingOrder = 100;
            copySr.enabled = true;
        }
        float padding = 0.2f;

        // UpdateBlockData(padding, copySr); legacy
        dragCopy.transform.localScale = transform.localScale;
        SetBlockVisible(false);
    }
    /// <summary>
    /// legacy.
    /// </summary>
    /// <param name="padding"></param>
    /// <param name="copySr"></param>
    private void UpdateBlockData(float padding, SpriteRenderer copySr)
    {
        if (blockType == BlockType.TextInput)
        {
            var textObj = dragCopy.transform.Find("TextInput").GetComponentInChildren<TMP_Text>();
            if (textObj != null)
            {
                textObj.text = textContent;
                textObj.ForceMeshUpdate();
                AdjustSpriteToTextBounds(textObj, copySr, padding);
            }
        }
        else if (blockType == BlockType.FormulaInput)
        {
            var dragFormulaInput = dragCopy.transform.Find("FormulaInput");
            if (dragFormulaInput != null)
            {
                var baseT = dragFormulaInput.Find("BaseText")?.GetComponent<TMP_Text>();
                var topT = dragFormulaInput.Find("TopExponent")?.GetComponent<TMP_Text>();
                var bottomT = dragFormulaInput.Find("BottomExponent")?.GetComponent<TMP_Text>();

                if (baseT) baseT.text = formulaBase;
                if (topT) topT.text = exponentTop;
                if (bottomT) bottomT.text = exponentBottom;

                baseT?.ForceMeshUpdate();
                topT?.ForceMeshUpdate();
                bottomT?.ForceMeshUpdate();

                AdjustSpriteToFormulaBounds(dragFormulaInput, copySr, padding);
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragCopy == null) return;

        Vector3 mouseWorld = Input.mousePosition; // Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        dragCopy.transform.position = mouseWorld;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragCopy == null) return;

        isDraging = false;

        Vector2 mouseWorldPos = Input.mousePosition; // Camera.main.ScreenToWorldPoint();
        Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos, emptyLayers);

        var empty = (EmptyController)null;

        if (hit != null && hit.TryGetComponent(out empty) && empty.PlacedBlock == null)
        {
            Debug.Log($"Блок {blockHashId} помещён на ячейку {empty.cellHashId}");

            if (blockHashId == empty.cellHashId)
            {
                Debug.Log("Блок правильно подставлен!");
                
            }
            else
            {
                Debug.Log("Хеш не совпадает!");

            }

            empty.PlacedBlock = this;

            transform.SetParent(empty.transform);
            transform.position = empty.transform.position;
        }
        else
        {
            transform.position = originalPosition;
            empty = null;
        }

        placedZone = empty;

        Destroy(dragCopy.gameObject);
        dragCopy = null;
        SetBlockVisible(true);
    }

    public void AddNewStructure(string name)
    {
        GameObject newObj = new GameObject(name);
        newObj.transform.SetParent(this.transform);
        blockStructures.Add(newObj.transform);
    }

    public bool Check()
    {
        if (!placedZone)
            throw new System.InvalidOperationException();

        return blockHashId == placedZone.cellHashId;
    }
    public void AnimateTrue()
    {
        Animate(Color.green);
    }
    public void AnimateFalse()
    {
        Animate(Color.red);
    }
    private void Animate(Color color)
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimatePlacement(color));
    }

    private IEnumerator AnimatePlacement(Color targetColor)
    {
        if (textGraphic == null) yield break;

        Color original = textNormalColor;
        float duration = 0.3f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            textGraphic.color = Color.Lerp(original, targetColor, t / duration);
            yield return null;
        }

        textGraphic.color = original;
    }

    private void AdjustSizeToContent()
    {
        if (sr == null) return;

        float padding = 0.2f;

        if (blockType == BlockType.TextInput)
        {
            if (textDisplay == null)
            {
                var textInput = transform.Find("TextInput");
                if (textInput != null)
                    textDisplay = textInput.GetComponentInChildren<TMP_Text>();

                if (textDisplay == null) return;
            }

            AdjustSpriteToTextBounds(textDisplay, sr ,padding);
        }
        else if (blockType == BlockType.FormulaInput)
        {
            var formulaInput = transform.Find("FormulaInput");
            AdjustSpriteToFormulaBounds(formulaInput, sr, padding);
        }
    }
    
    private void AdjustSpriteToTextBounds(TMP_Text tmp, SpriteRenderer targetSr, float padding = 0.2f)
    {
        if (tmp == null || targetSr == null) return;
        tmp.ForceMeshUpdate();

        // Получаем границы текста в локальных координатах
        var bounds = tmp.textBounds;
        Vector2 size = bounds.size + Vector3.one * padding;
        targetSr.size = size;
        targetSr.transform.localPosition = bounds.center;
        FitColliderToSprite();
    }
    private void AdjustSpriteToFormulaBounds(Transform formulaInput, SpriteRenderer targetSr, float padding = 0.2f)
    {
        var texts = formulaInput.GetComponentsInChildren<TMP_Text>(true);
        if (texts.Length == 0) return;

        Bounds bounds = texts[0].textBounds;
        Vector3 min = texts[0].rectTransform.localPosition + bounds.min;
        Vector3 max = texts[0].rectTransform.localPosition + bounds.max;

        foreach (var t in texts)
        {
            t.ForceMeshUpdate();
            var b = t.textBounds;
            Vector3 tMin = t.rectTransform.localPosition + b.min;
            Vector3 tMax = t.rectTransform.localPosition + b.max;
            min = Vector3.Min(min, tMin);
            max = Vector3.Max(max, tMax);
        }

        Vector2 size = (max - min);
        size += Vector2.one * padding;

        
        targetSr.size = size;
        targetSr.transform.localPosition = (min + max) / 2f;
        FitColliderToSprite();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        print("trigger");
        return;

        ReturnBlock();
    }

    public void ReturnBlock()
    {
        if (isDraging || !homeParent)
            return;

        if (transform.parent != homeParent)
            transform.SetParent(homeParent);

        transform.position = homeParent.position;

        placedZone = null;

        SetBlockVisible(true);
    }
}
