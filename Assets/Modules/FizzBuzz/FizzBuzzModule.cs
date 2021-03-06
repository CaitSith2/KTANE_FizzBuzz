using System.Collections.Generic;
using System.Linq;
using FizzBuzz;
using UnityEngine;

public class FizzBuzzModule : MonoBehaviour {

    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMAudio KMAudio;
    public KMSelectable[] SelectionButtons;
    public KMSelectable SubmitButton;
    public TextMesh[] Labels;
    public KMRuleSeedable RuleSeedModifier;

    private int[] ButtonStates = new int[3];
    private int[][] Nums = new int[3][];
    public int[] Colors = new int[3];
    private int[][] Solutions = new int[2][];
    private int moduleId;
    private static int moduleIdCounter = 1;
    private Color[] ColorMats = new Color[] { Color.red, Color.green, Color.cyan, Color.yellow, Color.white };

    // STATES
    // 0: Number
    // 1: Fizz
    // 2: Buzz
    // 3: FizzBuzz
    private static string[] StateNames = new string[] { "NUMBER", "FIZZ", "BUZZ", "FIZZBUZZ" };

    // COLORS
    // 0: Red
    // 1: Green
    // 2: Blue
    // 3: Yellow
    // 4: White
    private static string[] ColorNames = new string[] { "red", "green", "blue", "yellow", "white" };

    private int[,] table;
    private List<FizzBuzzRule> ruleSet;
    private int[] divisibilityRules;

    public float startingTime;
    public int startingModuleCount;

    void Start() {
        startingTime = BombInfo.GetTime();
        startingModuleCount = BombInfo.GetSolvableModuleNames().Count;

        MonoRandom ruleSeedRnd = RuleSeedModifier.GetRNG();
        FizzBuzzRuleGenerator generator = new FizzBuzzRuleGenerator(ruleSeedRnd, this);
        table = generator.GenerateOffsetTable();
        ruleSet = generator.GenerateRuleSet();
        divisibilityRules = generator.GenerateDivisibilityRules();

        moduleId = moduleIdCounter++;

        GetComponent<KMBombModule>().OnActivate += OnActivate;

        for (int i = 0; i < 3; i++) {
            var j = i;
            SelectionButtons[i].OnInteract += delegate { HandlePress(j); return false; };
            Nums[i] = GenNum();
            Labels[i].text = "";

            int color = Random.Range(0, 5);
            Colors[i] = color;
            Labels[i].color = ColorMats[color];
        }

        SubmitButton.OnInteract += delegate { Submit(); return false; };
    }

    void OnActivate() {
        for (int i = 0; i < 3; i++) {
            Labels[i].text = MakeString(i);
        }
        FindSolutions();
    }

    bool HandlePress(int buttonNum) {
        KMAudio.PlaySoundAtTransform("tick", this.transform);
        SelectionButtons[buttonNum].AddInteractionPunch();

        int state = (ButtonStates[buttonNum] + 1) % 4;
        ButtonStates[buttonNum] = state;

        switch (state) {
            case 0:
                Labels[buttonNum].text = MakeString(buttonNum);
                break;
            case 1:
                Labels[buttonNum].text = "Fizz";
                break;
            case 2:
                Labels[buttonNum].text = "Buzz";
                break;
            case 3:
                Labels[buttonNum].text = "FizzBuzz";
                break;
        }

        return false;
    }

    int[] GenNum() {
        int[] result = new int[7];
        for (int i = 0; i < 7; i++) {
            result[i] = Random.Range(0, 10);
        }
        return result;
    }

    string MakeString(int button) {
        string result = "";
        for (int i = 0; i < 7; i++) {
            result += "" + Nums[button][i];
        }
        return result;
    }

    void FindSolutions() {
        var conditions = new bool[6];

        for (int i = 0; i < 5; i++) {
            FizzBuzzRule rule = ruleSet[i];
            if (rule.CheckRule != null) {
                conditions[i] = rule.CheckRule(BombInfo);
                Debug.LogFormat("[FizzBuzz #{0}] Condition '{1}': {2}", moduleId, rule.Id, conditions[i]);
            } else {
                conditions[i] = rule.CheckRuleWithConfigs(BombInfo, rule.Configs);
                Debug.LogFormat("[FizzBuzz #{0}] Condition '{1}' ({3}): {2}", moduleId, rule.Id, conditions[i], string.Join(", ", rule.Configs.Take(2).Select(x => x.ToString()).ToArray()));
            }
        }

        if (conditions.All(b => !b)) {
            conditions[5] = true;
            Debug.LogFormat("[FizzBuzz #{0}] Condition '{1}': {2}", moduleId, "NoOtherConditionsMet", true);
        }

        foreach (var variableRule in new[] { false, true }) {
            var solution = new int[3];
            for (int n = 0; n < 3; n++) {
                int addNum = 0;

                // Add up the numbers for all conditions
                for (int i = 0; i < 6; i++) {
                    // If two strikes, add the number for “VariableRule” _instead of_ the one for “NoOtherConditionsMet”
                    if (i == 5 && variableRule)
                        addNum += table[6, Colors[n]];
                    else if (conditions[i])
                        addNum += table[i, Colors[n]];
                }

                int num = 0;
                int beforeNum = 0;
                for (int i = 0; i < 7; i++) {
                    beforeNum *= 10;
                    beforeNum += Nums[n][i];

                    num *= 10;
                    num += (Nums[n][i] + addNum) % 10;
                }

                solution[n] = (num % divisibilityRules[0] == 0 ? 1 : 0) + (num % divisibilityRules[1] == 0 ? 2 : 0);

                Debug.LogFormat("[FizzBuzz #{0}] Button {1} ({2}) [{3}: {4}]:", moduleId, n + 1, ColorNames[Colors[n]], ruleSet[5].Id, variableRule);
                Debug.LogFormat("[FizzBuzz #{0}] — original number is {1}", moduleId, beforeNum);
                Debug.LogFormat("[FizzBuzz #{0}] — adding number is {1}", moduleId, addNum);
                Debug.LogFormat("[FizzBuzz #{0}] — final number is {1}", moduleId, num);
                Debug.LogFormat("[FizzBuzz #{0}] — solution is {1}", moduleId, StateNames[solution[n]]);
            }
            Solutions[variableRule ? 1 : 0] = solution;
            Debug.LogFormat("[FizzBuzz #{0}] Solution for [{1}: {2}] is [{3}, {4}, {5}].", moduleId, ruleSet[5].Id, variableRule, StateNames[solution[0]], StateNames[solution[1]], StateNames[solution[2]]);
        }
    }

    void Submit() {
        KMAudio.PlaySoundAtTransform("tick", this.transform);
        GetComponent<KMSelectable>().AddInteractionPunch();

        var twoStrikes = BombInfo.GetStrikes() >= 2;
        Debug.LogFormat("[FizzBuzz #{0}] Submit button pressed at {1} 2 strikes.", moduleId, twoStrikes ? "≥" : "<");
        var solution = Solutions[twoStrikes ? 1 : 0];

        bool valid = true;
        for (int i = 0; i < 3; i++)
            if (solution[i] != ButtonStates[i])
                valid = false;

        if (valid) {
            Debug.LogFormat("[FizzBuzz #{0}] Module solved.", moduleId);
            BombModule.HandlePass();
        } else {
            Debug.LogFormat("[FizzBuzz #{0}] Submitted data: [{1}, {2}, {3}]. Incorrect solution.", moduleId, Labels[0].text, Labels[1].text, Labels[2].text);
            BombModule.HandleStrike();
        }
    }

    private string TwitchHelpMessage = "Press a button with !{0} press top (also t, 1, etc.). Press multiple buttons with !{0} press top middle bottom bottom. Submit with !{0} press submit. Submit particular answers with !{0} submit fizz number fizzbuzz";

    KMSelectable[] ProcessTwitchCommand(string command) {
        command = command.Trim().ToLowerInvariant();
        var pieces = command.Split(new[] { ' ', ',' }, System.StringSplitOptions.RemoveEmptyEntries);

        var list = new List<KMSelectable>();
        if (pieces[0] == "submit" && pieces.Length == 4) {
            for (int i = 1; i < pieces.Length; i++) {
                switch (pieces[i]) {
                    case "number":
                    case "num":
                    case "n":
                    case "nm":
                    case "#":
                        ChangeButtonState(list, i - 1, 0);
                        break;
                    case "fizz":
                    case "f":
                    case "fz":
                        ChangeButtonState(list, i - 1, 1);
                        break;
                    case "buzz":
                    case "b":
                    case "bz":
                        ChangeButtonState(list, i - 1, 2);
                        break;
                    case "fizzbuzz":
                    case "fzbz":
                    case "fb":
                        ChangeButtonState(list, i - 1, 3);
                        break;
                    default:
                        return null;
                }
            }
            list.Add(SubmitButton);
        } else if (pieces[0] == "press" && pieces.Length >= 2) {
            for (int i = 1; i < pieces.Length; i++) {
                switch (pieces[i]) {
                    case "t":
                    case "top":
                    case "u":
                    case "up":
                    case "upper":
                    case "1":
                    case "first":
                    case "1st":
                    case "one":
                        list.Add(SelectionButtons[0]);
                        break;

                    case "m":
                    case "middle":
                    case "c":
                    case "center":
                    case "centre":
                    case "2":
                    case "second":
                    case "2nd":
                    case "two":
                    case "mid":
                        list.Add(SelectionButtons[1]);
                        break;

                    case "b":
                    case "bottom":
                    case "d":
                    case "down":
                    case "l":
                    case "lower":
                    case "3":
                    case "third":
                    case "3rd":
                    case "three":
                        list.Add(SelectionButtons[2]);
                        break;

                    case "s":
                    case "sub":
                    case "submit":
                    case "done":
                        list.Add(SubmitButton);
                        break;

                    default:
                        return null;
                }
            }
        } else {
            return null;

        }

        return list.ToArray();
    }

    void ChangeButtonState(List<KMSelectable> list, int button, int value) {
        int prevBS = ButtonStates[button];
        while (prevBS != value) {
            list.Add(SelectionButtons[button]);
            prevBS++;
            prevBS %= 4;
        }
    }
}
