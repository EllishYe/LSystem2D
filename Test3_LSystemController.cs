using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[System.Serializable]
public class LSystemPreset
{
    public string name;          
    public string alphabet;      
    public string axiom;
    public string rules;         
    public int iterations;
    public float angle;
    public float step;
    public bool defaultGenerateFoliage; // foliage toggle
}

public class Test3_LSystemController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField alphabetInput;
    public TMP_InputField axiomInput;
    public TMP_InputField rulesInput;
    public TMP_InputField iterationInput;
    public Slider angleSlider;
    public Slider stepSlider;
    public Toggle foliageToggle;    // 新增：控制是否生成叶片

    [Header("Presets")]
    public TMP_Dropdown presetDropdown;
    public List<LSystemPreset> presets;

    [Header("Drawer")]
    public Test3_LsystemDrawer drawer;

    // L-System Data
    private Dictionary<string, string> rules = new Dictionary<string, string>();
    private List<string> alphabet = new List<string>();
    private string currentString = "";
    private int currentIteration = 0;
    private int maxIteration = 0;

    // EventTrigger 用于检测 PointerUp（step & angle）
    private EventTrigger stepEventTrigger;
    private EventTrigger.Entry stepPointerUpEntry;
    private EventTrigger angleEventTrigger;
    private EventTrigger.Entry anglePointerUpEntry;

    // 缓存当前滑块值
    private float currentStep;
    private float currentAngle;

    void Start()
    {
        bool hasPresets = (presetDropdown != null && presets != null && presets.Count > 0);

        // 初始化 stepSlider
        if (stepSlider != null)
        {
            stepSlider.onValueChanged.AddListener(OnStepSliderChanged);
            currentStep = (drawer != null) ? drawer.step : 1f;
            if (!hasPresets) StartCoroutine(InitStepSliderValue(currentStep));

            // 注册 PointerUp（确保场景有 EventSystem）
            stepEventTrigger = stepSlider.gameObject.GetComponent<EventTrigger>();
            if (stepEventTrigger == null) stepEventTrigger = stepSlider.gameObject.AddComponent<EventTrigger>();
            stepPointerUpEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            stepPointerUpEntry.callback.AddListener(OnStepSliderPointerUp);
            stepEventTrigger.triggers.Add(stepPointerUpEntry);
        }

        // angle slider 注册
        if (angleSlider != null)
        {
            angleSlider.onValueChanged.AddListener(OnAngleSliderChanged);
            currentAngle = angleSlider.value;
            if (!hasPresets) StartCoroutine(InitAngleSliderValue(currentAngle));

            angleEventTrigger = angleSlider.gameObject.GetComponent<EventTrigger>();
            if (angleEventTrigger == null) angleEventTrigger = angleSlider.gameObject.AddComponent<EventTrigger>();
            anglePointerUpEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            anglePointerUpEntry.callback.AddListener(OnAngleSliderPointerUp);
            angleEventTrigger.triggers.Add(anglePointerUpEntry);
        }

        // Preset 初始化（如果有）
        if (hasPresets)
        {
            presetDropdown.ClearOptions();
            List<string> options = new List<string>();
            foreach (var p in presets) options.Add(p.name);
            presetDropdown.AddOptions(options);
            presetDropdown.onValueChanged.AddListener(OnPresetSelected);

            // 直接应用第一个 Preset（会设置 sliders 并立即绘制）
            ApplyPreset(presets[0]);
        }
    }

    void OnDestroy()
    {
        if (stepSlider != null) stepSlider.onValueChanged.RemoveListener(OnStepSliderChanged);
        if (angleSlider != null) angleSlider.onValueChanged.RemoveListener(OnAngleSliderChanged);

        if (stepEventTrigger != null && stepPointerUpEntry != null) stepEventTrigger.triggers.Remove(stepPointerUpEntry);
        if (angleEventTrigger != null && anglePointerUpEntry != null) angleEventTrigger.triggers.Remove(anglePointerUpEntry);

        if (presetDropdown != null) presetDropdown.onValueChanged.RemoveListener(OnPresetSelected);
    }

    private IEnumerator InitStepSliderValue(float value)
    {
        yield return null;
        if (stepSlider != null) stepSlider.value = value; // 触发 OnStepSliderChanged（并同步 drawer.step）
    }

    private IEnumerator InitAngleSliderValue(float value)
    {
        yield return null;
        if (angleSlider != null) angleSlider.value = value; // 触发 OnAngleSliderChanged（仅缓存）
    }

    // 滑动时立即更新值（但不重绘场景）
    private void OnStepSliderChanged(float value)
    {
        currentStep = value;
        if (drawer != null)
        {
            drawer.step = value;
        }
        //Debug.Log($"[Controller] Step changed -> {value}");
    }

    private void OnAngleSliderChanged(float value)
    {
        currentAngle = value;
        //Debug.Log($"[Controller] Angle changed -> {value}");
    }

    // 只有在 PointerUp 时触发一次重绘
    private void OnStepSliderPointerUp(BaseEventData _)
    {
        Debug.Log("[Controller] Step PointerUp -> redraw");
        TriggerRedrawWithCurrentSettings();
    }

    private void OnAngleSliderPointerUp(BaseEventData _)
    {
        Debug.Log("[Controller] Angle PointerUp -> redraw");
        TriggerRedrawWithCurrentSettings();
    }

    // 公共重绘方法（使用 currentString 或 axiom）
    private void TriggerRedrawWithCurrentSettings()
    {
        if (drawer == null) return;

        // 同步 step（以防）
        if (stepSlider != null) drawer.step = stepSlider.value;

        string s = currentString;
        if (string.IsNullOrEmpty(s) && axiomInput != null) s = axiomInput.text.Trim();
        if (string.IsNullOrEmpty(s)) return;

        float angle = (angleSlider != null) ? angleSlider.value : currentAngle;
        // foliage toggle 同步
        if (foliageToggle != null) drawer.generateFoliage = foliageToggle.isOn;

        drawer.Draw(s, angle);
    }

    // Preset
    public void OnPresetSelected(int index)
    {
        if (presets == null || index < 0 || index >= presets.Count) return;
        ApplyPreset(presets[index]);
    }

    private void ApplyPreset(LSystemPreset p)
    {
        if (alphabetInput != null) alphabetInput.text = p.alphabet;
        if (axiomInput != null) axiomInput.text = p.axiom;
        if (rulesInput != null) rulesInput.text = p.rules;
        if (iterationInput != null) iterationInput.text = p.iterations.ToString();
        if (angleSlider != null) angleSlider.value = p.angle;
        if (stepSlider != null) stepSlider.value = p.step;
        if (foliageToggle != null) foliageToggle.isOn = p.defaultGenerateFoliage;

        if (drawer != null)
        {
            drawer.step = p.step;
            drawer.generateFoliage = p.defaultGenerateFoliage;
            drawer.Draw(p.axiom, p.angle);
        }

        // 更新内部状态
        alphabet.Clear();
        foreach (var s in p.alphabet.Split(',')) alphabet.Add(s.Trim());
        currentString = p.axiom;
        rules.Clear();
        string[] lines = p.rules.Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] parts = line.Split(new string[] { "->" }, System.StringSplitOptions.None);
            if (parts.Length == 2) rules[parts[0].Trim()] = parts[1].Trim();
        }
        maxIteration = p.iterations;
        currentIteration = 0;
    }

    // Generate / Iterate
    public void OnGenerate()
    {
        alphabet.Clear();
        foreach (var s in alphabetInput.text.Split(',')) alphabet.Add(s.Trim());

        currentString = axiomInput.text.Trim();

        rules.Clear();
        string[] lines = rulesInput.text.Split(';');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] parts = line.Split(new string[] { "->" }, System.StringSplitOptions.None);
            if (parts.Length == 2) rules[parts[0].Trim()] = parts[1].Trim();
        }

        int.TryParse(iterationInput.text, out maxIteration);
        currentIteration = 0;

        if (drawer != null)
        {
            if (stepSlider != null) drawer.step = stepSlider.value;
            if (foliageToggle != null) drawer.generateFoliage = foliageToggle.isOn;
            drawer.Draw(currentString, angleSlider != null ? angleSlider.value : currentAngle);
        }
    }

    public void OnIterate()
    {
        if (currentIteration >= maxIteration) return;
        currentString = ProcessOneIteration(currentString);
        currentIteration++;
        if (drawer != null)
        {
            if (stepSlider != null) drawer.step = stepSlider.value;
            if (foliageToggle != null) drawer.generateFoliage = foliageToggle.isOn;
            drawer.Draw(currentString, angleSlider != null ? angleSlider.value : currentAngle);
        }
    }

    private string ProcessOneIteration(string input)
    {
        string result = "";
        int i = 0;
        while (i < input.Length)
        {
            bool matched = false;
            foreach (var a in alphabet)
            {
                if (input.Substring(i).StartsWith(a))
                {
                    result += rules.ContainsKey(a) ? rules[a] : a;
                    i += a.Length;
                    matched = true;
                    break;
                }
            }
            if (!matched) { result += input[i]; i++; }
        }
        return result;
    }
}
