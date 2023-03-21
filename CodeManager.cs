using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;


public class CodeManager : MonoBehaviour
{
    GameObject copier;
    string name;

    /*** TODO:::     
        when deleting: give option to edit: precoded other options
        Curr:
        David: write up 10-20 example projects/exercises (#, title, 1 sentence desc, 2-4 comments)
        Editting:
        Brian: Clean up editor mode, add pretty lines/animations
    ***/

    Vector2 old_position;
    bool not_over_UI;
    float tap_duration = 0;


    /* MAIN TEXT DISPLAY */
    public GameObject Display_Text;




    //public float text_height = 62.75f; // 62.75 for 75, 41.75 for 50,
    public float text_tab_width = 105f;

    public Scrollbar UIElement_CompilationSpeed;
    /* Options Menu */
    public GameObject UIElement_OptionsMenu;
    bool UI_optionsMenuOpen;


    public Dropdown UIElement_LoadList;
    /* Compiler Menu */
    public GameObject UIElement_CompilationMenu;
    /* Console variables*/
    string Console_output = "Console:\n";
    bool on_console = true;
    public Text Output_text;
    public Text Debug_text;


    bool allowed_to_edit;
    bool editting_locked = true;
    public GameObject UIElement_EdittingMenu;
    bool editting_mode;
    int editting_tabAmount;
    int editting_line;
    string editting_line_type;
    string editting_variable_type;
    bool tapping_over_same_line;
    bool entering_string_literal;
    bool entering_string;
    int line_shift;

    bool just_editted = false;

    public GameObject UIElement_InputField;

    /**
     * 
     * Calculate these in real time: check every line above current for variable declaration, skipping } and every line until a { + 1
     * 
     * */
    public List<string> integers_within_editting_scope = new List<string>();
    public List<string> doubles_within_editting_scope = new List<string>();
    public List<string> booleans_within_editting_scope = new List<string>();
    public List<string> strings_within_editting_scope = new List<string>();
    string[] available_variable_types;

    public List<string> lines;

    string[] list_of_PDTs = { "boolean", "char", "double", "int", "String" };
    string[] list_of_evaluations = { "==", ">", "<", "<=", ">=", "!=" };
    //string[] list_of_operations = { " + ", " - ", " * ", " / ", " % "};
    string[] list_of_condensed_operations = { "=", "++", "--", "+=", "-=", "*=", "/=", "%=" };

    bool next_else = false; //Used for if/else logic
    string loop_condition;
    string loop_variable_modifer;

    float time_delayer; //counter for compiling speed

    /* Initializing code (C++ified) */
    void Start()
    {
        copier = GameObject.Find("CopyField");
        copier.SetActive(false);
        /* Load blank C++ Arduino document */
        lines = new List<string>();
        Display_LoadScript("NewClass");
        Display_pushText();
    }
    /* All code that requires running per frame (animation calls, button presses */
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            line_shift++;

            Display_Text.transform.position = new Vector2(0,42f * Display_Text.transform.localScale.y * line_shift);
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            line_shift--;

            Display_Text.transform.position = new Vector2(0, 42f * Display_Text.transform.localScale.y * line_shift);
        }


        Display_pushText();
        float text_height = 42f * Display_Text.transform.localScale.y;
        int current_line = (int)((Screen.height - Input.mousePosition.y) / text_height);
        float snap_to_line = current_line * text_height;
        float part_of_line = (Screen.height - Input.mousePosition.y) / text_height - Mathf.Round((Screen.height - Input.mousePosition.y) / text_height);
        just_editted = false;
        current_line += line_shift;
        if (Input.GetMouseButtonDown(0) && editting_mode)
        {
            for (int i = 0; i < UIElement_EdittingMenu.transform.childCount; i++)
            {
                if (UIElement_EdittingMenu.transform.GetChild(i).gameObject.activeSelf)
                {
                    //  print(UIElement_EdittingMenu.transform.GetChild(i).GetChild(0).gameObject.GetComponent<Text>().text);
                    if (UI_clickCheck(-562.5f/2, 562.5f / 2, -50f, 50f, Input.mousePosition, UIElement_EdittingMenu.transform.GetChild(i).transform.position))
                    {
                        not_over_UI = false;
                        Editor_selectOption(UIElement_EdittingMenu.transform.GetChild(i).GetChild(0).gameObject.GetComponent<Text>().text);
                        just_editted = true;
                        break;
                    }
                }
            }
        }

        if (!UIElement_EdittingMenu.activeInHierarchy && !UIElement_InputField.activeInHierarchy)
        {
            GameObject.Find("EdittingPointer").GetComponent<Image>().color = new Color(1, 1, 1, .5f);
        }
        else
        {
            GameObject.Find("EdittingPointer").GetComponent<Image>().color = new Color(1, 1, 1, 0f);

        }
        if (Input.GetMouseButtonDown(0))
        {
            old_position = Input.mousePosition;
            not_over_UI = true;
            if (UI_optionsMenuOpen) not_over_UI = false;
            /*Editting Locker*/
            //if (UI_clickCheck(0, 200, -200, 0, old_position, UIElement_EdittingLocker.transform.position)) { not_over_UI = false; UI_setColor(UIElement_EdittingLocker, new Color(1, 0, 0)); Editor_toggleLock(); }
            /*Options Menu*/
            if (UI_clickCheck(0, 1000, 0, 200, old_position, UIElement_OptionsMenu.transform.position))
            { not_over_UI = false; if (UI_clickCheck(0, 200, 0, 200, old_position, UIElement_OptionsMenu.transform.position)) { UI_setColor(UIElement_OptionsMenu, new Color(1, 0, 0)); Display_OptionsMenu(); } }

            tapping_over_same_line = true;
        }
        if ((part_of_line) > -.35f && (part_of_line) < 0)
        {
            GameObject.Find("EdittingPointer").GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width / 2, 15);
            if (Input.GetMouseButtonDown(0) && current_line < lines.Count && not_over_UI)
            {
                if (current_line+1 < lines.Count && lines[current_line+1].Contains("{"))
                {
                    GameObject.Find("EdittingPointer").GetComponent<Image>().color = new Color(1, 0, 0, 1f);
                }
                else if (!UIElement_EdittingMenu.activeInHierarchy && !UIElement_InputField.activeInHierarchy)
                {
                    if (!just_editted) lines.Insert(current_line + 1, "");
                }
                else
                {

                }
            }
            snap_to_line = snap_to_line + 10 + text_height/2;
        }
        else
        {
            GameObject.Find("EdittingPointer").GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width / 2, 40);
            if (Input.GetMouseButtonDown(0) && current_line < lines.Count && not_over_UI && !UIElement_EdittingMenu.activeInHierarchy && !UIElement_InputField.activeInHierarchy)
            { 
                if ((lines[current_line].Contains("void") || lines[current_line].Contains("{") || lines[current_line].Contains("}")))
                {
                    GameObject.Find("EdittingPointer").GetComponent<Image>().color = new Color(1, 0, 0, 1f);
                }
                else 
                {
                   if (!just_editted) Editor_start(current_line);
                }
            }



        }
        //float snap_to_line = (int)((Screen.height - Input.mousePosition.y) / text_height) * text_height;
        GameObject.Find("EdittingPointer").GetComponent<RectTransform>().position = new Vector2(0, Screen.height - snap_to_line );
        GameObject.Find("EdittingPointer").transform.GetChild(0).gameObject.GetComponent<Text>().text = (int)((Screen.height - Input.mousePosition.y) / text_height) + "";

        allowed_to_edit = true;
        if (UIElement_InputField.activeSelf == true) allowed_to_edit = false;

        //!!
        //font size 50 = 41.75 pixels  !!!!
        //!!
        
       
        if (false && Input.GetMouseButtonUp(0))
        {
            if (tap_duration < .25f && tapping_over_same_line)
            {
                //short tap
                if (allowed_to_edit && not_over_UI && !editting_mode && !UI_optionsMenuOpen)
                {
                    int line = (int)((Screen.height - Input.mousePosition.y + Display_Text.GetComponent<RectTransform>().anchoredPosition.y) / text_height + 1);
                    if (line < lines.Count - 1)
                    {
                            print(lines[line]);
                            if (lines[line - 1].IndexOf("for") == 0 || lines[line - 1].IndexOf("if") == 0 || lines[line - 1].IndexOf("while") == 0)
                            {
                                line++;
                            }
                    }
                }
            }
            if (tap_duration > .25f && tapping_over_same_line)
            {
                if (allowed_to_edit && not_over_UI && !editting_mode && !UI_optionsMenuOpen)
                {
                    int line = (int)((Screen.height - Input.mousePosition.y + Display_Text.GetComponent<RectTransform>().anchoredPosition.y) / text_height);
                    print(lines[line]);
                    if (lines[line].Contains("public class"))
                    {
                        //if (!editting_locked)
                        //{
                         //   UI_setColor(UIElement_EdittingLocker, new Color(1, 0, 0));
                            //  Display_pushText(lines, new int[0]);
                        //}
                        //else
                       // {
                            //UIElement_EdittingMenu.SetActive(true);
                            Editor_setOptions(new string[] { "Enter the class's name:" });
                            editting_line_type = "SetClassName";
                        //}
                    }
                    int line_of_main_method = 0;
                    while (lines[line_of_main_method].Contains("public static void") == false)
                    {
                        line_of_main_method++;
                    }
                    if (line > ++line_of_main_method && line < lines.Count - 1)
                    {
                       // if (!editting_locked)
                        //{
                         //   UI_setColor(UIElement_EdittingLocker, new Color(1, 0, 0));
                            // Display_pushText(lines, new int[0]);
                        //}
                        //else
                        //{
                            if (lines[line].IndexOf("if") == 0 || lines[line].IndexOf("while") == 0 || lines[line].IndexOf("for") == 0)
                            {
                                lines.RemoveAt(line);
                                lines.RemoveAt(line);
                                int matching_bracket = 0;
                                for (int i = line; i < lines.Count; i++)
                                {
                                    if (lines[i].IndexOf("{") == 0)
                                    {
                                        matching_bracket++;
                                    }
                                    if (lines[i].IndexOf("}") == 0)
                                    {
                                        if (matching_bracket == 0)
                                        {
                                            lines.RemoveAt(i);
                                            break;
                                        }
                                        matching_bracket--;
                                    }
                                }
                            }
                            else if (lines[line].Contains("}") == false && lines[line].Contains("{") == false)
                            {
                                lines.RemoveAt(line);
                            }
                        //}
                    }

                }
            }
            tap_duration = 0;
        }
        if (UI_optionsMenuOpen)
        {
            //UIElement_OptionsMenu.GetComponent<Image>().sprite = (Resources.Load("Sprites/SettingsOpen") as GameObject).GetComponent<SpriteRenderer>().sprite;
            UIElement_OptionsMenu.transform.GetChild(0).gameObject.SetActive(true);
            UI_setTowardsRectTransform(UIElement_OptionsMenu, -800, -25);
        }
        else
        {
            //UIElement_OptionsMenu.GetComponent<Image>().sprite = (Resources.Load("Sprites/SettingsClose") as GameObject).GetComponent<SpriteRenderer>().sprite;
            UIElement_OptionsMenu.transform.GetChild(0).gameObject.SetActive(false);
            UI_setTowardsRectTransform(UIElement_OptionsMenu, 0, -25);
        }
        if (editting_mode)
        {
            //UI_setTowardsRectTransform(Display_Text.gameObject, ((editting_tabAmount) * -1 * text_tab_width) - 100f, Display_Text.GetComponent<RectTransform>().anchoredPosition.y);
            UIElement_EdittingMenu.GetComponent<RectTransform>().sizeDelta = new Vector2(lines[editting_line].Length * 120f + 300f, 410f);
            UIElement_EdittingMenu.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        }


        UI_resetColor(UIElement_OptionsMenu, .025f);
        UI_resetColor(UIElement_OptionsMenu.transform.GetChild(0).gameObject, .025f);
        //    UI_resetColor(UIElement_EdittingLocker, .025f);

        UI_resetColor(UIElement_EdittingMenu.transform.GetChild(0).gameObject, .025f);

        UI_resetColor(UIElement_EdittingMenu.transform.GetChild(1).gameObject, .025f);

        UI_resetColor(UIElement_EdittingMenu.transform.GetChild(2).gameObject, .025f);

        UI_resetColor(UIElement_EdittingMenu.transform.GetChild(3).gameObject, .025f);
        
    }
    /* Check for button press (manual button) */
    public bool UI_clickCheck(float xMin, float xMax, float yMin, float yMax, Vector2 mousePosition, Vector2 otherPosition)
    {
        if (mousePosition.x + xMin < otherPosition.x && mousePosition.x + xMax > otherPosition.x) if (mousePosition.y + yMin < otherPosition.y && mousePosition.y + yMax > otherPosition.y) return true;
        return false;
    }
    /* UI animation assisting methods */
    public void UI_moveRectTransform(GameObject item, float x, float y)
    {
        Vector2 pos = item.GetComponent<RectTransform>().anchoredPosition;
        item.GetComponent<RectTransform>().anchoredPosition = new Vector2(pos.x + x, pos.y + y);
    }
    public void UI_setRectTransform(GameObject item, float x, float y)
    {
        item.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
    }
    public void UI_setTowardsRectTransform(GameObject item, float targetX, float targetY)
    {
        Vector2 pos = item.GetComponent<RectTransform>().anchoredPosition;
        if (Mathf.Abs(pos.x - targetX) < .1f && Mathf.Abs(pos.y - targetY) < .1f)
        {
            if (!(pos.x == targetX && pos.y == targetY)) UI_setRectTransform(item, targetX, targetY);
        }
        else
        {
            float style_smoothness = 10f;
            item.GetComponent<RectTransform>().anchoredPosition = new Vector2(pos.x - (pos.x - targetX) / style_smoothness, pos.y - (pos.y - targetY) / style_smoothness);
        }
    }
    /* UI color animation assiting methods */
    public void UI_setColor(GameObject item, Color color)
    {
        item.GetComponent<Image>().color = color;
    }
    public void UI_resetColor(GameObject item, float animation_speed)
    {
        Color current = item.GetComponent<Image>().color;
        if (current.r < 1) current.r += animation_speed;
        if (current.r > 1) current.r = 1;
        if (current.g < 1) current.g += animation_speed;
        if (current.g > 1) current.g = 1;
        if (current.b < 1) current.b += animation_speed;
        if (current.b > 1) current.b = 1;
        item.GetComponent<Image>().color = current;
    }
    /* Editor initializing method */
    public void Editor_start(int line)
    {
        editting_line = line;
        editting_mode = true;
        editting_tabAmount = 0;

        for (int i = 0; i < line; i++)
        {
            if (lines[i].IndexOf("{") == 0)
            {
                editting_tabAmount++;
            }
            if (lines[i].IndexOf("}") == 0)
            {
                editting_tabAmount--;
            }
        }
       // print(editting_tabAmount);
       // lines.Insert(line, "");
      //  lines.Insert(line, " ");
      //  lines.Insert(line, "");

        UIElement_EdittingMenu.SetActive(true);
      //  UI_setRectTransform(UIElement_EdittingMenu, -5, -Screen.height / 2);//(line + 1) * -41.75f + 12f);
        //UI_setRectTransform(Display_Text.gameObject,0, (line + 1) * text_height - 511);
        editting_line_type = "";
        Editor_findVariablesWithinScope(line);

        /*  Variable
         *      -Create new variable
         *          Types
         *      -Modify existing variable [only if exists in scope]
         *  Flow Control
         *      -If
         *      -Else [only if immediately after an if block]
         *      -Switch
         *      -While
         *      -For
         *  Method
         *      -Serial
         *          Begin, Println, Print
         *      -Delay
         *      -pinMode
         *      -digitalRead
         *      -analogWrite
         *      -Make [only if outside  other methods]
         * 
         */
        Editor_setOptions(new string[] { "New Line", "Delete Line", "Cancel",});
        //Editor_setOptions(new string[] { "Create Variable", "Modify Variable", "Flow Control", "Method" });
    }
    /* Editor closing method */
    public void Editor_end()
    {
        editting_line = -1;
        editting_mode = false;

        editting_line_type = "";
        UIElement_EdittingMenu.SetActive(false);
    }
    /* Sketchy code: does typing/entering of code into program from user input */
    public void Editor_findVariablesWithinScope(int line)
    {
        booleans_within_editting_scope = new List<string>();
        integers_within_editting_scope = new List<string>();
        doubles_within_editting_scope = new List<string>();
        strings_within_editting_scope = new List<string>();
        for (int i = 0; i < line; i++)
        {
            for (int types_of_PDTs = 0; types_of_PDTs < list_of_PDTs.Length; types_of_PDTs++)
            {
                if (lines[i].Contains(list_of_PDTs[types_of_PDTs] + " "))
                {
                    if (lines[i].Contains("for (" + list_of_PDTs[types_of_PDTs] + " ") || lines[i].IndexOf(list_of_PDTs[types_of_PDTs] + " ") == 0)
                    {
                        string name = lines[i].Substring(lines[i].IndexOf(list_of_PDTs[types_of_PDTs] + " ") + list_of_PDTs[types_of_PDTs].Length + 1);
                        if (name.IndexOf(" ") < name.IndexOf(";")) name = name.Remove(name.IndexOf(" "));
                        else name = name.Remove(name.IndexOf(";"));
                        switch (types_of_PDTs)
                        {
                            case 0: //Add new integer definition to list
                                booleans_within_editting_scope.Add(name);
                                break;
                            case 2: //Add new double definition to list
                                doubles_within_editting_scope.Add(name);
                                break;
                            case 3: //Add new integer definition to list
                                integers_within_editting_scope.Add(name);
                                break;
                            case 4: //Add new string definition to list
                                strings_within_editting_scope.Add(name);
                                break;
                        }
                    }
                }
            }
        }
        List<string> availables = new List<string>();
        if (integers_within_editting_scope.Count != 0) availables.Add("Integer");
        if (doubles_within_editting_scope.Count != 0) availables.Add("Double");
        if (booleans_within_editting_scope.Count != 0) availables.Add("Boolean");
        if (strings_within_editting_scope.Count != 0) availables.Add("String");
        available_variable_types = availables.ToArray();
    }
    public void Editor_setOptions(string[] options)
    {
        Editor_setOptionPositions(options);
        entering_string_literal = false;
        entering_string = false;
        for (int i = 0; i < options.Length; i++)
        {
            if (i == 0)
            {
                if (options[i] == "Enter a name:" || options[i] == "Enter a string:" || options[i] == "Enter the class's name:")
                {
                    entering_string = true;
                    if (options[i] == "Enter a string:") entering_string_literal = true;
                    UIElement_InputField.SetActive(true);
                    UIElement_InputField.GetComponent<InputField>().contentType = InputField.ContentType.Standard;
                    UIElement_InputField.GetComponent<InputField>().placeholder.GetComponent<Text>().text = options[i];
                    UIElement_EdittingMenu.transform.GetChild(i).gameObject.SetActive(false);
                }
                else if (options[i] == "Enter a number:")
                {
                    UIElement_InputField.SetActive(true);
                    if (editting_variable_type == "Integer") UIElement_InputField.GetComponent<InputField>().contentType = InputField.ContentType.IntegerNumber;
                    else if (editting_variable_type == "Double") UIElement_InputField.GetComponent<InputField>().contentType = InputField.ContentType.DecimalNumber;
                    UIElement_InputField.GetComponent<InputField>().placeholder.GetComponent<Text>().text = options[i];
                    UIElement_EdittingMenu.transform.GetChild(i).gameObject.SetActive(false);
                }
                else
                {
                    UIElement_InputField.SetActive(false);
                    Editor_setOption(i, options[i]);
                    UIElement_EdittingMenu.transform.GetChild(i).gameObject.SetActive(true);
                }
            }
            else
            {
                Editor_setOption(i, options[i]);
                UIElement_EdittingMenu.transform.GetChild(i).gameObject.SetActive(true);

            }
        }
        for (int i = options.Length; i < UIElement_EdittingMenu.transform.childCount; i++)
        {
            UIElement_EdittingMenu.transform.GetChild(i).gameObject.SetActive(false);
        }
    }
    public void Editor_setOption(int option, string text)
    {
        UIElement_EdittingMenu.transform.GetChild(option).GetChild(0).gameObject.GetComponent<Text>().text = text;
    }
    public void Editor_finishTyping()
    {
        string input = UIElement_InputField.GetComponent<InputField>().text;
        if (entering_string)
        {
            input = input.Replace(' ', '_');

            if (entering_string_literal)
            {
                input = "\"" + input + "\"";
            }
        }
        //UIElement_EdittingMenu.transform.GetChild(0).GetChild(1).gameObject.GetComponent<InputField>().text;
        UIElement_InputField.GetComponent<InputField>().text = "";
        UIElement_InputField.SetActive(false);
        //UIElement_EdittingMenu.transform.GetChild(0).GetChild(1).gameObject.GetComponent<InputField>().text = "";
        if (editting_line_type == "SetClassName")
        {
            int line = 0;
            while (lines[line].Contains("/*") == false)
            {
                line++;
            }
            line++;
            lines[line] = "" + input;
            Display_pushText();

        }
        if (editting_line_type.Contains("Write") && !editting_line_type.Contains("Analog Write") && !editting_line_type.Contains("Digital Write")) 
        {
            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + input + ")";
            Editor_setOptions(new string[] { "Finish" });
        }
        if (editting_line_type.Contains("Attach"))
        {
            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + input + ")";
            Editor_setOptions(new string[] { "Finish" });
        }
        if (editting_line_type.Contains("Pin Mode"))
        {
            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + input + ", )";
            Editor_setOptions(new string[] {"INPUT", "OUTPUT" });
        }
        if (editting_line_type.Contains( "Map") && editting_line_type.Length > 6)
        {
            editting_line_type += "p";
            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + input + ")";
            Editor_setOptions(new string[] { "Finish" });
        }
        else if (editting_line_type.Contains("Map"))
        {
            editting_line_type += "p";
            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + input + ", )";
            Editor_setOptions(new string[] { "Number" });
        }

        if (editting_line_type.Contains("Digital Write"))
        {
            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + input + ", )";
            Editor_setOptions(new string[] { "HIGH", "LOW"});
        }
        if (editting_line_type.Contains("Delay"))
        {
            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + input + ")";
            Editor_setOptions(new string[] { "Finish" });
        }
        if (editting_line_type.Contains("Digital Read"))
        {
            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + input + ")";
            Editor_setOptions(new string[] { "Finish" });
        }
        if (editting_line_type == "Analog Write")
        {
            editting_line_type = "Analog Write 2";
               lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + input + ", )";
            string[] answersAWri = new string[integers_within_editting_scope.Count + 1];
            answersAWri[0] = "Number";
            for (int i = 0; i < integers_within_editting_scope.Count; i++)
            {
                answersAWri[i + 1] = integers_within_editting_scope[i];
            }
            Editor_setOptions(answersAWri);
        }
        else if (editting_line_type == "Analog Write 2")
        {
            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + input + ")";
            Editor_setOptions(new string[] { "Finish" });
        }

        if (editting_line_type.Contains("Analog Read"))
        {
            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + input + ")";
            Editor_setOptions(new string[] { "Finish" });
        }
        if (editting_line_type.Contains("rint"))
        {
            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + input + ")";
            Editor_setOptions(new string[] { "Finish" });
        }
        if (editting_line_type.Contains("If"))
        {
            if (editting_line_type == "IfLeftSide") Editor_setOptions(new string[] { "Comparison" });
            else Editor_setOptions(new string[] { "Finish" });

            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + input + ")";

        }
        if (editting_line_type.Contains("Modify"))
        {
            if (editting_line_type.IndexOf("ForModify") == 0)
            {
                lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + input + ")";
                Editor_setOptions(new string[] { "Finish" });

            }
            else if (editting_line_type.Contains("For"))
            {
                lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + input + ")";
                Editor_setOptions(new string[] { "Modifier" });
            }
            else
            {
                if (editting_line_type == "ModifyString")
                {
                    lines[editting_line] += input;
                }
                else lines[editting_line] += input;
                Editor_setOptions(new string[] { "Finish" });
            }
        }

        if (editting_line_type == "ForSetVariable")
        {
            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + input + ")";
            editting_line_type = "ForSetVariableModify" + editting_variable_type;

            Editor_setOptions(new string[] { "Comparison" });

        }

        string[] options;
        if (editting_line_type == "CreateInteger" || (editting_line_type == "ForNameVariable" && editting_variable_type == "Integer"))
        {
            if (editting_line_type == "ForNameVariable")
            {
                lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + input + " = )";
                editting_line_type = "ForSetVariable";
                integers_within_editting_scope.Add(input);
            }
            else
            {
                lines[editting_line] += input + " = ";
                editting_line_type = "ModifyInteger";
            }
            if (integers_within_editting_scope.Count != 0)
            {
                options = new string[integers_within_editting_scope.Count + 1];
                options[0] = "Number";
                //options[1] = "Method";
                for (int i = 0; i < integers_within_editting_scope.Count; i++)
                {
                    options[i + 1] = integers_within_editting_scope[i];
                }
                Editor_setOptions(options);
            }
            else Editor_setOptions(new string[] { "Number" });
        }
        if (editting_line_type == "CreateDouble" || (editting_line_type == "ForNameVariable" && editting_variable_type == "Double"))
        {
            if (editting_line_type == "ForNameVariable")
            {
                lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + input + " = )";
                editting_line_type = "ForSetVariable";
                doubles_within_editting_scope.Add(input);
            }
            else
            {
                lines[editting_line] += input + " = ";
                editting_line_type = "ModifyDouble";
            }
            if (doubles_within_editting_scope.Count != 0)
            {
                options = new string[doubles_within_editting_scope.Count + 2];
                options[0] = "Number";
                options[1] = "Method";
                for (int i = 0; i < doubles_within_editting_scope.Count; i++)
                {
                    options[i + 2] = doubles_within_editting_scope[i];
                }
                Editor_setOptions(options);
            }
            else Editor_setOptions(new string[] { "Method", "Number" });
        }
        if (editting_line_type == "CreateBoolean")
        {
            editting_line_type = "ModifyBoolean";
            lines[editting_line] += input + " = ";

            if (booleans_within_editting_scope.Count != 0) Editor_setOptions(new string[] { "Variable", "True", "False" });
            else Editor_setOptions(new string[] { "True", "False" });
        }
        if (editting_line_type == "CreateString")
        {
            editting_line_type = "ModifyString";
            lines[editting_line] += input + " = ";

            if (strings_within_editting_scope.Count != 0) Editor_setOptions(new string[] { "Variable", "String Literal" });
            else Editor_setOptions(new string[] { "String Literal" });
        }
        // Display_pushText(lines, new int[0]);

    }
    public void Editor_selectOption(string selection)
    {

        switch (selection)
        {

            case "New Line":
                Editor_setOptions(new string[] { "Create Variable", "Modify Variable", "Flow Control", "Method" });
                if (name == "Servos")
                {
                    Editor_setOptions(new string[] { "Create Variable", "Modify Variable", "Flow Control", "Method", "Servos" });
                }
                break;
            case "Delete Line":
                lines.RemoveAt(editting_line);
                Editor_end();
                break;
            case "Cancel":
                Editor_end();
                break;
            case "Flow Control":
                if (editting_line_type == "") { Editor_setOptions(new string[] { "Delay","If"/*, "Else", "While", "For"*/ }); editting_line_type = selection; }
                break;
            case "Servos":
                Editor_setOptions(new string[] { "Map" ,"servo1", "servo2" });
                break;
            case "servo1":
                editting_line_type = selection;
                lines[editting_line] = "servo1";
                Editor_setOptions(new string[] { "Attach", "Write" });
                break;
            case "servo2":
                editting_line_type = selection;
                lines[editting_line] = "servo2";
                Editor_setOptions(new string[] { "Attach", "Write" });
                break;
            case "Attach":
                editting_line_type = selection;
                lines[editting_line] += ".attach()";
                Editor_setOptions(new string[] { "Number" });
                break;
            case "Write":
                editting_line_type = selection;
                lines[editting_line] += ".write()";
                string[] answersWri = new string[integers_within_editting_scope.Count + 1];
                answersWri[0] = "Number";
                for (int i = 0; i < integers_within_editting_scope.Count; i++)
                {
                    answersWri[i + 1] = integers_within_editting_scope[i];
                }
                Editor_setOptions(answersWri);
                break;
            case "Map":
                editting_line_type = selection;
                lines[editting_line] += "map()";
                string[] answersMap = new string[integers_within_editting_scope.Count + 1];
                answersMap[0] = "Number";
                for (int i = 0; i < integers_within_editting_scope.Count; i++)
                {
                    answersMap[i + 1] = integers_within_editting_scope[i];
                }
                Editor_setOptions(answersMap);

                break;
            case "Method":
                if (editting_line_type == "")
                {
                    //voids
                    editting_line_type = selection; lines[editting_line] = selection;
                    Editor_setOptions(new string[] { "Serial", "Pin Mode", "Digital Write", "Analog Write" });
                }
                else
                {
                    if (editting_line_type == "If")
                    {
                        Editor_setOptions(new string[] {"Digital Read", "Analog Read"});
                    }
                    //numbers (iterate == temperature probe)
                    //Editor_setOptions(new string[] { "Math", "Iterate", });
                }
                if (editting_line_type.Contains("ModifyInteger"))
                {
                    editting_line_type = selection;
                    Editor_setOptions(new string[] { "Digital Read", "Analog Read", "Map"});
                }
                break;
            case "Create Variable":
                editting_line_type = "CreateVariable";
                Editor_setOptions(new string[] { "Integer", "Double", "Boolean", "String" });
                break;
            case "Modify Variable":
                editting_line_type = "ModifyVariable";
                if (available_variable_types.Length > 0) Editor_setOptions(available_variable_types);
                else
                {
                    UI_setColor(UIElement_EdittingMenu.transform.GetChild(1).gameObject, new Color(1, 0, 0));
                    print("no variables");
                }
                break;




            case "Variable":
                if (editting_line_type == "") { Editor_setOptions(available_variable_types); editting_line_type = "ModifyVariable"; }
                if (editting_line_type == "Keyword") { Editor_setOptions(new string[] { "Integer", "Boolean" }); editting_line_type = "CreateVariable"; }
                if (editting_line_type == "If") { Editor_setOptions(available_variable_types); editting_line_type = "IfLeftSide"; }
                if (editting_line_type == "IfLeftSide") { Editor_setOptions(available_variable_types); editting_line_type = "IfLeftSide"; }
                if (editting_line_type == "IfRightSide") { Editor_setOptions(available_variable_types); editting_line_type = "IfRightSide"; }
                if (editting_line_type == "Print") { Editor_setOptions(available_variable_types); }
                break;
            case "Conditional":
                if (editting_line_type == "Keyword") { editting_line_type = selection; lines[editting_line] = selection; }
                Editor_setOptions(new string[] { "If", "Else" });
                break;
            case "Loop":
                if (editting_line_type == "Keyword") editting_line_type = selection;
                Editor_setOptions(new string[] { "While Loop", "For Loop" });
                break;
            case "Serial":
                lines[editting_line] = selection;
                Editor_setOptions(new string[] { "Begin", "Print", "Print Line" });
                break;
            case "Delay":
                
                    editting_line_type = selection;
                lines[editting_line] = "delay()";
                Editor_setOptions(new string[] { "Number" });
                
                break;
            case "Digital Read":
                lines[editting_line] += "digitalRead()";
                if (editting_line_type == "")
                {
                    editting_line_type = selection;
                    Editor_setOptions(new string[] { "Number" });
                }
                else if (editting_line_type == "If")
                {
                    editting_line_type = "If Digital";
                    Editor_setOptions(new string[] { "Number" });
                }
                else
                {

                }
                break;
            case "Digital Write":
                editting_line_type = selection;
                lines[editting_line] = "digitalWrite()";
                string[] answersDW = new string[integers_within_editting_scope.Count + 1];
                answersDW[0] = "Number";
                for (int i = 0; i < integers_within_editting_scope.Count; i++)
                {
                    answersDW[i + 1] = integers_within_editting_scope[i];
                }
                Editor_setOptions(new string[] { "Number" });
                break;
            case "Analog Read":
                editting_line_type = selection;
                lines[editting_line] += "analogRead()";
                string[] answersAR = new string[integers_within_editting_scope.Count + 1];
                answersAR[0] = "Number";
                for (int i = 0; i < integers_within_editting_scope.Count; i++)
                {
                    answersAR[i + 1] = integers_within_editting_scope[i];
                }
                Editor_setOptions(answersAR);
                break;
            case "Analog Write":
                editting_line_type = selection;
                lines[editting_line] = "analogWrite()";
                string[] answersAW = new string[integers_within_editting_scope.Count + 1];
                answersAW[0] = "Number";
                for (int i = 0; i < integers_within_editting_scope.Count; i++)
                {
                    answersAW[i + 1] = integers_within_editting_scope[i];
                }
                Editor_setOptions(answersAW);
                break;
            case "Pin Mode":
                editting_line_type = selection;
                lines[editting_line] = "pinMode()";
                string[] answers = new string[integers_within_editting_scope.Count + 1];
                answers[0] = "Number";
                for (int i = 0; i < integers_within_editting_scope.Count; i++)
                {
                    answers[i+1] = integers_within_editting_scope[i];
                }        
                Editor_setOptions(answers);
                break;

            case "Random Value":
                if (editting_line_type.Contains("If") || editting_line_type.Contains("Print"))
                {
                    lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + "Math.random())";
                }
                else
                {
                    lines[editting_line] += "Math.random()";
                }
                Editor_setOptions(new string[] { "Finish" });
                break;
            case "Number":
                Editor_setOptions(new string[] { "Enter a number:" });
                break;
            case "String Literal":
                Editor_setOptions(new string[] { "Enter a string:" });
                break;
            case "Integer":
                if (editting_line_type == "CreateVariable") { Editor_setOptions(new string[] { "Enter a name:" }); lines[editting_line] = "int "; editting_line_type = "CreateInteger"; }
                else if (editting_line_type == "For") { Editor_setOptions(new string[] { "Enter a name:" }); lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + "int )"; editting_line_type = "ForNameVariable"; }
                else Editor_setOptions(integers_within_editting_scope.ToArray());
                editting_variable_type = "Integer";
                break;
            case "Double":
                if (editting_line_type == "CreateVariable") { Editor_setOptions(new string[] { "Enter a name:" }); lines[editting_line] = "double "; editting_line_type = "CreateDouble"; }
                else if (editting_line_type == "For") { Editor_setOptions(new string[] { "Enter a name:" }); lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + "double )"; editting_line_type = "ForNameVariable"; }
                else Editor_setOptions(doubles_within_editting_scope.ToArray());
                editting_variable_type = "Double";
                break;
            case "Boolean":
                if (editting_line_type == "CreateVariable") { Editor_setOptions(new string[] { "Enter a name:" }); lines[editting_line] = "boolean "; editting_line_type = "CreateBoolean"; }
                else Editor_setOptions(booleans_within_editting_scope.ToArray());
                editting_variable_type = "Boolean";
                break;
            case "String":
                if (editting_line_type == "CreateVariable") { Editor_setOptions(new string[] { "Enter a name:" }); lines[editting_line] = "String "; editting_line_type = "CreateString"; }
                else Editor_setOptions(strings_within_editting_scope.ToArray());
                editting_variable_type = "String";
                break;
            case "True":
                if (editting_line_type.Contains("If"))
                {
                    lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + "true)";
                }
                else lines[editting_line] += "true";
                Editor_setOptions(new string[] { "Finish" });
                break;
            case "False":
                if (editting_line_type.Contains("If"))
                {
                    lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + "false)";
                }
                else lines[editting_line] += "false";
                Editor_setOptions(new string[] { "Finish" });
                break;
            case "HIGH":
                lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + selection + ")";
                Editor_setOptions(new string[] { "Finish" });
                break;
            case "LOW":
                lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + selection + ")";
                Editor_setOptions(new string[] { "Finish" });
                break;
            case "INPUT":
                lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + selection + ")";
                Editor_setOptions(new string[] { "Finish" });
                break;
            case "OUTPUT":
                lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + selection + ")";
                Editor_setOptions(new string[] { "Finish" });
                break;
            case "If":
                editting_line_type = "If";
                lines[editting_line] = "if ()";
                Editor_setOptions(new string[] { "Variable", "Method", "String Literal", "Number" });
                break;
            case "While":
                editting_line_type = "If";
                Editor_setOptions(new string[] { "Variable", "Method", "String Literal", "Number" });
                lines[editting_line] = "while ()";
                break;
            case "For":
                editting_line_type = "For";
                Editor_setOptions(new string[] { "Integer", "Double" });
                lines[editting_line] = "for ()";
                break;
            //case "Operator":
            //    if (editting_variable_type == "String") Editor_setOptions(new string[] { " + " });
            //    else Editor_setOptions(list_of_operations);
            //    break;
            case "Comparison":
                if (editting_variable_type == "Boolean" || editting_variable_type == "String") Editor_setOptions(new string[] { "==", "!=" });
                else if (editting_line_type.Contains("ForSet"))
                {
                    lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + "; )";
                    editting_line_type = "ForComparisonModify" + editting_variable_type;
                    Editor_findPossibleValuesForVariables();
                }
                else Editor_setOptions(list_of_evaluations);
                break;
            case "Begin":
                editting_line_type = "Begon";
                lines[editting_line] = "Serial.begin(9600)";
                Editor_setOptions(new string[] { "Finish" });
                break;
            case "Print":
                editting_line_type = "Print";
                lines[editting_line] = "Serial.print()";
                Editor_setOptions(new string[] { "Variable", "String Literal", "Number" });
                break;
            case "Print Line":
                editting_line_type = "Print";
                lines[editting_line] = "Serial.println()";
                Editor_setOptions(new string[] { "Variable", "String Literal", "Number" });
                break;
            case "Modifier":
                lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + "; )";
                editting_line_type = "ForModify" + editting_variable_type;
                if (editting_variable_type == "Integer")
                {
                    Editor_setOptions(integers_within_editting_scope.ToArray());
                }
                if (editting_variable_type == "Double")
                {
                    Editor_setOptions(doubles_within_editting_scope.ToArray());
                }
                break;
            case "Finish":
                //if (editting_line_type.IndexOf("Modify") == 0 || editting_line_type == "Print") 
                if (editting_line_type.Contains("If") || editting_line_type.Contains("For") || editting_line_type.Contains("While"))
                { lines.Insert(editting_line + 2, "{"); lines.Insert(editting_line + 3, "}"); }
                else lines[editting_line] += ";";
                Editor_end();
                break;
        }

        for (int i = 0; i < integers_within_editting_scope.Count; i++)
        {
            if (selection == integers_within_editting_scope[i])
            {
                Editor_usedAVariable(selection);
            }
        }
        for (int i = 0; i < doubles_within_editting_scope.Count; i++)
        {
            if (selection == doubles_within_editting_scope[i])
            {
                Editor_usedAVariable(selection);
            }
        }
        for (int i = 0; i < booleans_within_editting_scope.Count; i++)
        {
            if (selection == booleans_within_editting_scope[i])
            {
                Editor_usedAVariable(selection);
            }
        }
        for (int i = 0; i < strings_within_editting_scope.Count; i++)
        {
            if (selection == strings_within_editting_scope[i])
            {
                Editor_usedAVariable(selection);
            }
        }
        for (int i = 0; i < list_of_evaluations.Length; i++)
        {
            if (selection == list_of_evaluations[i])
            {
                if (editting_line_type == "IfLeftSide")
                {
                    editting_line_type = "IfRightSide";
                    lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + " " + selection + " )";
                    if (editting_variable_type == "Boolean")
                    {
                        if (booleans_within_editting_scope.Count != 0) Editor_setOptions(new string[] { "Variable", "True", "False" });
                        else Editor_setOptions(new string[] { "True", "False" });
                    }
                    else if (editting_variable_type == "String")
                    {
                        if (strings_within_editting_scope.Count != 0) Editor_setOptions(new string[] { "Variable", "String Literal" });
                        else Editor_setOptions(new string[] { "String Literal" });
                    }
                    else Editor_setOptions(new string[] { "Variable", "Method", "Number" });
                }
                if (editting_line_type.Contains("For"))
                {
                    lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + " " + selection + " )";
                    Editor_setOptions(new string[] { "Variable", "Method", "Number" });

                }
            }
        }
        for (int i = 0; i < list_of_condensed_operations.Length; i++)
        {
            if (selection == list_of_condensed_operations[i])
            {
                if (editting_line_type.Contains("For"))
                {
                    if (selection == "++" || selection == "--")
                    {
                        lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + selection + ")";
                        Editor_setOptions(new string[] { "Finish" });
                    }
                    else
                    {
                        lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + " " + selection + " )";

                        Editor_findPossibleValuesForVariables();
                    }
                }
                else
                {
                    if (selection == "++" || selection == "--")
                    {
                        lines[editting_line] += selection;
                        Editor_setOptions(new string[] { "Finish" });
                    }
                    else
                    {
                        lines[editting_line] += " " + selection + " ";
                        Editor_findPossibleValuesForVariables();
                    }
                }
            }
        }


    }
    public void Editor_findPossibleValuesForVariables()
    {
        string[] options;
        if (editting_line_type.Contains("ModifyInteger"))
        {
            if (integers_within_editting_scope.Count != 0)
            {
                options = new string[integers_within_editting_scope.Count + 2];
                options[0] = "Number";
                options[1] = "Method";
                //options[1] = "Method";
                for (int j = 0; j < integers_within_editting_scope.Count; j++)
                {
                    options[j + 2] = integers_within_editting_scope[j];
                }
                Editor_setOptions(options);
            }
            else Editor_setOptions(new string[] { "Number", "Method" });
        }
        if (editting_line_type.Contains("ModifyDouble"))
        {
            if (doubles_within_editting_scope.Count != 0)
            {
                options = new string[doubles_within_editting_scope.Count + 2];
                options[0] = "Number";
                options[1] = "Method";
                for (int j = 0; j < doubles_within_editting_scope.Count; j++)
                {
                    options[j + 2] = doubles_within_editting_scope[j];
                }
                Editor_setOptions(options);
            }
            else Editor_setOptions(new string[] { "Method", "Number" });
        }
        if (editting_line_type == "ModifyBoolean")
        {
            if (booleans_within_editting_scope.Count != 0) Editor_setOptions(new string[] { "Variable", "True", "False" });
            else Editor_setOptions(new string[] { "True", "False" });
        }
        if (editting_line_type == "ModifyString")
        {
            if (strings_within_editting_scope.Count != 0) Editor_setOptions(new string[] { "Variable", "String Literal" });
            else Editor_setOptions(new string[] { "String Literal" });
        }
    }
    public void Editor_usedAVariable(string selection)
    {
        if (editting_line_type == "Analog Write 2")
        {
            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + selection + ")";
            Editor_setOptions(new string[] { "Finish" });
        }

        else if (editting_line_type == "Write")
        {
            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + selection + ")";
            Editor_setOptions(new string[] { "Finish" });
        }
        if (editting_line_type == "Map")
        {
            editting_line_type = "Map1";
            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + selection + ", )";
            Editor_setOptions(new string[] { "Number" });
        }
        if (editting_line_type == "Pin Mode")
        {
            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + selection + ", )";
            Editor_setOptions(new string[] {"INPUT", "OUTPUT" });
        }
        if (editting_line_type == "IfLeftSide")
        {
            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + selection + ")";
            print(editting_variable_type);
            if (editting_variable_type == "Boolean") Editor_setOptions(new string[] { "Comparison" });
            else Editor_setOptions(new string[] { "Comparison" });
        }
        if (editting_line_type == "IfRightSide")
        {
            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + selection + ")";
            if (editting_variable_type == "Boolean") Editor_setOptions(new string[] { "Finish" });
            else Editor_setOptions(new string[] { "Finish" });
        }
        if (editting_line_type == "Print")
        {
            lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + selection + ")";
            if (editting_variable_type == "Boolean") Editor_setOptions(new string[] { "Finish" });
            else Editor_setOptions(new string[] { "Finish" });
        }
        if (editting_line_type.Contains("Modify"))
        {
            if (editting_line_type != "ModifyVariable")
            {
                if (editting_line_type.Contains("For"))
                {
                    lines[editting_line] = lines[editting_line].Substring(0, lines[editting_line].Length - 1) + selection + ")";
                    Editor_setOptions(new string[] { "Comparison" });
                }
                else
                {
                    lines[editting_line] += selection;
                    if (editting_variable_type == "Boolean") Editor_setOptions(new string[] { "Finish" });
                    else Editor_setOptions(new string[] { "Finish" });
                }
            }
            //this one is below cuz it would otherwise trigger both loops ;)
            if (editting_line_type == "ModifyVariable")
            {
                editting_line_type = "Modify" + editting_variable_type;
                lines[editting_line] = selection;
                if (editting_variable_type == "Boolean") Editor_setOptions(new string[] { "=" });
                else if (editting_variable_type == "String") Editor_setOptions(new string[] { "=", "+=" });
                else Editor_setOptions(list_of_condensed_operations);
                print("yes");
            }
        }
        if (editting_line_type.Contains("ForModify"))
        {
            Editor_setOptions(list_of_condensed_operations);
        }
    }
    public void Editor_setOptionPositions(string[] options)
    {
        int spacing = 220;
        int space = spacing - spacing * options.Length % 2, count = 0;
        for (int i = options.Length / 2; i >= -options.Length / 2 + (options.Length + 1) % 2; i--)
        {
            if (options.Length % 2 == 1) UIElement_EdittingMenu.transform.GetChild(count).gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(200, i * (spacing * 2) - space + spacing);
            else UIElement_EdittingMenu.transform.GetChild(count).gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(200, i * (spacing * 2) - space);
            count++;
        }
    }
    /* Lock to prevent accidental editting of code */
    public void Editor_toggleLock()
    {
        editting_locked = !editting_locked;
      //  UIElement_EdittingLocker.GetComponent<Image>().sprite = (Resources.Load("Sprites/editting" + editting_locked) as GameObject).GetComponent<SpriteRenderer>().sprite;
    }




    void Display_pushText()
    {
        string output = "";
        for (int i = 0; i < lines.Count; i++)
        {
            if (i < 9) output += "0";
            output += (i + 1) + "\t" +  ColorCoder.colorize( lines[i] ) + "\n";
        }
        Display_Text.GetComponent<Text>().text = output;//Interpreter_Object.ToString();
    }

    /* Display UI (C++ified)*/
    public void Display_ToCopyableText()
    {
        string output = "";
        for (int i = 0; i < lines.Count; i++)
        { 
            output += lines[i] + "\n";
        }

        copier.SetActive(!copier.activeSelf);
        copier.GetComponent<InputField>().text = output;
    }
    public void Display_OptionsMenu()
    {
        UI_optionsMenuOpen = !UI_optionsMenuOpen;
    }
    public void Display_LoadScript(string name)
    {
        //Name of class prompt
      //  Editor_setOptions(new string[] { "Enter the class's name:" });
      //  editting_line_type = "SetClassName";
        //Pull data from resources folder (blank document syntax)
      
        if (name == "") name = GameObject.Find("LabelDropdown").GetComponent<Text>().text;
        if (name == "") name = "NewClass";
        this.name = name;
        TextAsset textFile = Resources.Load(name) as TextAsset;
        //Populating inline variables
        string[] lines = textFile.text.Split('\n');
        this.lines = new List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            this.lines.Add(lines[i]);
        }
        //Pushing populated variables
        Display_pushText();
        Display_Text.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
    }

}