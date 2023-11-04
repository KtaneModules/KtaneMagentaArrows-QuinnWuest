using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class MagentaArrowsScript : MonoBehaviour
{
    public KMNeedyModule Needy;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public TextMesh ScreenText;

    public GameObject[] ArrowObjs;
    public Material[] ArrowMats;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _isActivated;

    public KMSelectable[] ArrowSels;

    private static readonly string[] _arrowDirs = new string[] { "UP", "RIGHT", "DOWN", "LEFT" };

    private int[][] _arrowVals = new int[4][]
    {
        new int[7] { 0, 15, 19, 16, 24, 5, 10 },
        new int[6] { 8, 13, 1, 6, 17, 25 },
        new int[7] { 2, 21, 18, 9, 14, 4, 23 },
        new int[6] { 7, 20, 11, 12, 22, 3 },
    };
    private int[] _screenVals = new int[2];
    private int _selectedArrow;
    private int _pressIx;
    private int[] _correctArrows = new int[2];

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        Needy.OnNeedyActivation += Activate;
        Needy.OnNeedyDeactivation += Deactivate;
        BombInfo.OnBombSolved += BombSolve;

        Needy.OnTimerExpired += delegate ()
        {
            Debug.LogFormat("[Magenta Arrows #{0}] Ran out of time. Strike.", _moduleId);
            _pressIx = 0;
            Needy.HandleStrike();
            Deactivate();
        };
        for (int i = 0; i < ArrowSels.Length; i++)
            ArrowSels[i].OnInteract += ArrowPress(i);

        ScreenText.text = "--";
    }

    private KMSelectable.OnInteractHandler ArrowPress(int arrow)
    {
        return delegate ()
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ArrowSels[arrow].transform);
            ArrowSels[arrow].AddInteractionPunch(0.5f);
            if (!_isActivated)
                return false;
            if (arrow == _correctArrows[_pressIx])
            {
                Debug.LogFormat("[Magenta Arrows #{0}] Pressed {1} correctly.", _moduleId, _arrowDirs[arrow]);
                if (_pressIx == 0)
                    _pressIx++;
                else
                {
                    _pressIx = 0;
                    Needy.HandlePass();
                    Deactivate();
                }
            }
            else
            {
                Debug.LogFormat("[Magenta Arrows #{0}] Pressed {1} when {2} was expected. Strike.", _moduleId, _arrowDirs[arrow], _arrowDirs[_correctArrows[_pressIx]]);
                _pressIx = 0;
                Needy.HandleStrike();
                Deactivate();
            }
            return false;
        };
    }

    private void Activate()
    {
        _isActivated = true;
        _screenVals = Enumerable.Range(0, 26).ToArray().Shuffle().Take(2).ToArray();
        ScreenText.text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[_screenVals[0]].ToString() + "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[_screenVals[1]].ToString();
        Debug.LogFormat("[Magenta Arrows #{0}] The display shows {1}.", _moduleId, ScreenText.text);
        _selectedArrow = Rnd.Range(0, 4);
        for (int i = 0; i < ArrowObjs.Length; i++)
        {
            if (_selectedArrow == i)
                ArrowObjs[i].GetComponent<MeshRenderer>().material = ArrowMats[1];
            else
                ArrowObjs[i].GetComponent<MeshRenderer>().material = ArrowMats[0];
        }
        for (int i = 0; i < 4; i++)
        {
            if (_arrowVals[i].Contains(_screenVals[0]))
                _correctArrows[0] = i;
            if (_arrowVals[i].Contains(_screenVals[1]))
                _correctArrows[1] = i;
        }
        Debug.LogFormat("[Magenta Arrows #{0}] Before reorientation, the correct arrow presses are {1} {2}.", _moduleId, _arrowDirs[_correctArrows[0]], _arrowDirs[_correctArrows[1]]);
        Debug.LogFormat("[Magenta Arrows #{0}] The highlighted arrow is the {1} arrow.", _moduleId, _arrowDirs[_selectedArrow]);
        _correctArrows[0] = (_correctArrows[0] + (4 - _selectedArrow)) % 4;
        _correctArrows[1] = (_correctArrows[1] + (4 - _selectedArrow)) % 4;
        Debug.LogFormat("[Magenta Arrows #{0}] After reorientation, the correct arrow presses are {1} {2}.", _moduleId, _arrowDirs[_correctArrows[0]], _arrowDirs[_correctArrows[1]]);
    }

    private void Deactivate()
    {
        _isActivated = false;
        Needy.HandlePass();
        ScreenText.text = "--";
        for (int i = 0; i < ArrowObjs.Length; i++)
            ArrowObjs[i].GetComponent<MeshRenderer>().material = ArrowMats[0];
    }

    private void BombSolve()
    {
        StartCoroutine(BombSolveAnimation());
    }

    private IEnumerator BombSolveAnimation()
    {
        for (int i = 0; i < 30; i++)
        {
            ScreenText.text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[Rnd.Range(0, 26)].ToString() + " ";
            yield return new WaitForSeconds(0.05f);
        }
        for (int i = 0; i < 30; i++)
        {
            ScreenText.text = "G" + "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[Rnd.Range(0, 26)];
            yield return new WaitForSeconds(0.05f);
        }
        ScreenText.text = "GG";
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} up/right/down/left [Presses the specified arrow button] | Words can be substituted as one letter (Ex. right as r)";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        var m = Regex.Match(command, @"^\s*(up|u|right|r|down|d|left|l|)\s+(up|u|right|r|down|d|left|l|)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            yield break;
        var btns = new List<KMSelectable>();
        for (int i = 1; i <= 2; i++)
        {
            if (m.Groups[i].Value == "up" || m.Groups[i].Value == "u")
                btns.Add(ArrowSels[0]);
            else if (m.Groups[i].Value == "right" || m.Groups[i].Value == "r")
                btns.Add(ArrowSels[1]);
            else if (m.Groups[i].Value == "down" || m.Groups[i].Value == "d")
                btns.Add(ArrowSels[2]);
            else if (m.Groups[i].Value == "left" || m.Groups[i].Value == "l")
                btns.Add(ArrowSels[3]);
            else
                yield break;
        }
        yield return null;
        yield return btns;
    }
}
