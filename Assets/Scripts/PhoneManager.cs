using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PhoneManager : MonoBehaviour
{
    private GameManager manager;

    [SerializeField] private TMP_Text phoneNumberText;
    [SerializeField] private TMP_Text statusText;

    public List<AudioClip> audioClips;

    public bool IsPowered { get; private set; } = false;

    private int RandomButtonSoundIndex => Random.Range(0, 10);
    private string phoneNumber = "";

    private void Awake()
    {
        manager = FindObjectOfType<GameManager>(true);

        DisplayStatusText("");
        DisplayPhoneNumber();
    }

    private void Update()
    {
        if (!IsPowered)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Button(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Button(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Button(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Button(3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Button(4);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Button(5);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Button(6);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            Button(7);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            Button(8);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            Button(9);
        }
        else if (Input.GetKeyDown(KeyCode.Backspace))
        {
            ButtonBackspace();
        }
    }

    public void Button(int index)
    {
        if (!IsPowered)
        {
            return;
        }

        if (phoneNumber.Length < 10)
        {
            phoneNumber += index;
            DisplayPhoneNumber();
            //manager.LocalPlayer.CmdPlayAudioClipOnPlayer(index, manager.LocalPlayer.netId);
        }
    }
    public void ButtonBackspace()
    {
        if (!IsPowered)
        {
            return;
        }

        if (phoneNumber.Length > 0)
        {
            phoneNumber = phoneNumber[..(phoneNumber.Length - 1)];
            DisplayPhoneNumber();
            //manager.LocalPlayer.CmdPlayAudioClipOnPlayer(RandomButtonSoundIndex, manager.LocalPlayer.netId);
        }
    }
    public void ButtonTogglePower()
    {
        IsPowered = !IsPowered;
        if (!IsPowered)
        {
            phoneNumber = "";
            DisplayStatusText("");
            DisplayPhoneNumber();
        }
        else
        {
            DisplayStatusText("Enter a number...");
            DisplayPhoneNumber();
        }
        //manager.LocalPlayer.CmdPlayAudioClipOnPlayer(RandomButtonSoundIndex, manager.LocalPlayer.netId);
    }
    public void ButtonCall()
    {
        if (!IsPowered)
        {
            return;
        }

        //manager.LocalPlayer.CmdPlayAudioClipOnPlayer(RandomButtonSoundIndex, manager.LocalPlayer.netId);
    }
    public void ButtonEndCall()
    {
        if (!IsPowered)
        {
            return;
        }

        //manager.LocalPlayer.CmdPlayAudioClipOnPlayer(RandomButtonSoundIndex, manager.LocalPlayer.netId);
    }
    
    private void DisplayStatusText(string status)
    {
        statusText.text = status;
    }
    private void DisplayPhoneNumber()
    {
        phoneNumberText.text = phoneNumber;
    }
}
