using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class BlockController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Grid Settings")]
    public int row = 0;
    public int column = 0;
    public int width = 1;
    public int height = 1;

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

    private LevelController levelController;
    private Vector3 originalPosition;

    private TMP_Text textDisplay;
    private TMP_Text baseText;
    private TMP_Text topExponent;
    private TMP_Text bottomExponent;

    void Start()
    {
        levelController = FindFirstObjectByType<LevelController>();
        AlignToGrid();
        UpdateBlockDisplay();
    }

    void AlignToGrid()
    {
        if (levelController == null) return;

        float cellSize = levelController.cellSize;
        transform.position = new Vector3(column * cellSize, row * cellSize, 0f);
        transform.localScale = new Vector3(width * cellSize, height * cellSize, 1f);
    }

    void UpdateBlockDisplay()
    {
        Transform textInput = transform.Find("TextInput");
        Transform formulaInput = transform.Find("FormulaInput");

        bool isText = blockType == BlockType.TextInput;
        bool isFormula = blockType == BlockType.FormulaInput;

        if (textInput) textInput.gameObject.SetActive(isText);
        if (formulaInput) formulaInput.gameObject.SetActive(isFormula);

        if (isText && textInput)
        {
            textDisplay = textInput.GetComponentInChildren<TMP_Text>();
            if (textDisplay) textDisplay.text = textContent;
        }

        if (isFormula && formulaInput)
        {
            baseText = formulaInput.Find("BaseText")?.GetComponent<TMP_Text>();
            topExponent = formulaInput.Find("TopExponent")?.GetComponent<TMP_Text>();
            bottomExponent = formulaInput.Find("BottomExponent")?.GetComponent<TMP_Text>();

            if (baseText) baseText.text = formulaBase;
            if (topExponent) topExponent.text = exponentTop;
            if (bottomExponent) bottomExponent.text = exponentBottom;
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying == false)
        {
            levelController = FindFirstObjectByType<LevelController>();
            AlignToGrid();
            UpdateBlockDisplay();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = transform.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        transform.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y, 0);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        CheckOverlapWithEmptyCell();
    }

    private void CheckOverlapWithEmptyCell()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);

        if (hit != null)
        {
            EmptyController empty = hit.GetComponent<EmptyController>();
            if (empty != null)
            {
                Debug.Log($"Ѕлок {blockHashId} помещЄн на €чейку {empty.cellHashId}");
                if (blockHashId == empty.cellHashId)
                {
                    Debug.Log("Ѕлок правильно подставлен!");
                }
                else
                {
                    Debug.Log("’еш не совпадает!");
                }
                return;
            }
        }

        Debug.Log("Ѕлок не попал ни в одну €чейку");
        transform.position = originalPosition;
    }
}
