using System;
using System.Collections.Generic;
using System.Windows.Forms;

using System.Text;
using System.IO;
using System.IO.Ports;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Microsoft;

namespace PIC
{
    public static class CenCom
    {
        public static bool bLogMaster = false;
        public static int iWait = 1;
        public static string sOS = "XP";
        public static int iProtocol = 2;
        public static int nothing = 1;
        public static bool displayBill = true;
        public static bool requestCancel = false;
        public static int inputType = 1;
        public static int bEmMode = 0;

        [DllImport("Kernel32.dll")]
        public static extern bool SetLocalTime(ref SYSTEMTIME Time);

        public static bool bBillRequest = false;
        public static bool bCardRequest = false;

        public static bool bPrintRequest = false;
        public static bool bTestRequest = false;
        public static bool bStatusRequest = false;
        public static bool bPrintWait = false;

        public static bool bPreSwipe = false;

        public static bool bPreInput = false;

        static int iScreenMode;
        static int iScreen;

        public static string sPump;
        public static string sPICID;
        public static int iPICID;

        public static string sInput;//so display can initialize
        public static string sStatus; //to report status (system test)
        static string sCode;
        public static string sPINEntry = "";

        public static int iNumWashes;

        static int iStatusCheckCount=5;//wait...
        static int iOverallStatus=0;
        public static bool bForceCheck = false;

        public static bool bSpanish = false;
        public static bool bScreen1Spanish = false;

        public static bool bTestMode = false;
        public static bool bAlarmOn = false;
        public static bool bAlarmEnabled = false;
        public static bool bMainDoorOpen = false;
        public static bool bVaultDoorOpen = false;

        public static int iBeep = 0;//number of beeps
        static bool bBeepOn = false;

        public static int iMsgBoxTimer = 0;
        public static int iScreenTimer = 0;

        public static int iMonitoringLevel = 0;

        public static int i485Monitor = 0;
        public static int iPrintTimer = 0;
        public static bool bForceScreenChange = false;//PD- to distinguish cancel press to know if user wants to leave get receipt screen vs. ssc causing too early a timeout to screen 1
        public static bool bStartup = true;
        public static bool bCashTransaction = false;
        public static int iPrinterWait = 5;

        //public static bool bGeneratePINBlock = false;

        static int iCurrentSetting;
        public static int iTempEncrptionType;

        public static int iLoggingSSC = 0;
        public static int iLoggingEPP = 0;
        public static int iLoggingCA = 0;
        public static int iLoggingCR = 0;
        public static int iLoggingPR = 0;

        public static int iTempLoggingSSC;
        public static int iTempLoggingEPP;
        public static int iTempLoggingCA;
        public static int iTempLoggingCR;
        public static int iTempLoggingPR;

        public static int iBrand;
        public static int iTempBrand;
        public static int iTempPtrType;
        public static bool bPINGenerated = false;

        static int iEPPStatus = 0xFF;
        static int iCRStatus = 0xFF;
        static int iCAStatus = 0xFF;
        static int iPRStatus = 0xFF;
        static string[] sBox = new string[255];

        static TimerCallback timerDelegate = new TimerCallback(CheckStatus);
        static TimerCallback timerDelegate2 = new TimerCallback(CheckBeep);
        //static AutoResetEvent autoEvent = new AutoResetEvent(false);
        //static System.Threading.Timer stateTimer = new System.Threading.Timer(timerDelegate, autoEvent, 1000, 250);
        static System.Threading.Timer stateTimer = new System.Threading.Timer(timerDelegate, null, 1000, 1000);
        static System.Threading.Timer beepTimer = new System.Threading.Timer(timerDelegate2, null, 50, 50);

        public struct SYSTEMTIME
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;

            public void FromDateTime(string sTime)
            {
                wYear = Convert.ToUInt16("20" + sTime.Substring(0, 2));
                wMonth = Convert.ToUInt16(sTime.Substring(2, 2));
                wDay = Convert.ToUInt16(sTime.Substring(4, 2));
                wHour = Convert.ToUInt16(sTime.Substring(6, 2));
                wMinute = Convert.ToUInt16(sTime.Substring(8, 2));
                wSecond = Convert.ToUInt16(sTime.Substring(10, 2));
            }
        }
        public static void ChangeDateTime(string sNewTime)
        {
            SYSTEMTIME st = new SYSTEMTIME();

            st.FromDateTime(sNewTime);
            //Call Win32 API to set time
            SetLocalTime(ref st);
        }

        //private static string GetMachineGUID()
        //{
            //try
            //{


            //    string x64Result = string.Empty;
            //    string x86Result = string.Empty;

            //    string location = @"SOFTWARE\Microsoft\Cryptography";
            //    string name = "MachineGuid";

            //    using (RegistryKey localMachineX64View = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            //    {

            //    } 


            //}
            //catch (Exception)
            //{
            //}
            //string registryValue = string.Empty;
            //RegistryKey localKey = null;
            //if (Environment.Is64BitOperatingSystem)
            //{
            //    localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
            //}
            //else
            //{
            //    localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry32);
            //}

            //try
            //{
            //    localKey = localKey.OpenSubKey(@"Software\\MyKey");
            //    registryValue = localKey.GetValue("TestKey").ToString();
            //}
            //catch (NullReferenceException nre)
            //{

            //}
            //return registryValue;
        //}

        public static void StartUp()
        {
            Debug.WriteLine("OS = " + sOS);

            if (sOS == "CE")
            {
                FileAccessCE.ReadFile("\\SDCard\\SPT\\settings.txt");
                FileAccessCE.ParseSettings();
            }
            else
            {
                FileAccessXP.ReadSettings(); 
            }







            Display.Init();

            EPP.Init();//PD- wait until msg box below disappears otherwise new readsettings code will cause app to crash (port nums not set in time)
            CardReader.Init();
            CashAcceptor.Init();
            Printer.Init();
            RS485.Init();

            Reset();

            Crc.InitTable();

            Display.GotoScreen(0,0);

            Display.ShowMessageBox("Please wait while loading...", iWait);//PD - rev 19
        }

        public static void Reset()
        {
            displayBill = true;
            bBillRequest = false;
            bCardRequest = false;
            Debug.WriteLine("******************bCardRequest1= " + bCardRequest);

            RS485.sDataToSend = "";
            sPump = "";
            sInput = "";
            //sCode = "";PD- rev23
        }

        public static void CheckBeep(Object stateInfo)
        {
            if (iBeep > 0 && bBeepOn == false)
            {
                iBeep--;
                bBeepOn = true;
                BeepOn();
            }
            else if (bBeepOn == true)
            {
                bBeepOn = false;
                BeepOff();
            }
        }

        public static void CheckStatus(Object stateInfo)
        {
            if (iMsgBoxTimer > 0)
            {
                iMsgBoxTimer--;
                Debug.WriteLine("MSG BOX TIMER:  ->  " + iMsgBoxTimer);


                if (iMsgBoxTimer == 0)
                {
                    Display.HideMessageBox();
                    if (bStartup)
                    {
                        //bStartup = false;//PD- turn off after components are initialized to prevent crash

                        EPP.Init();
                        CardReader.Init();
                        CashAcceptor.Init();
                        Printer.Init();
                        RS485.Init();
                    }
                }
            }
            else if (iScreenTimer > 0)
            {
                iScreenTimer--;
                Debug.WriteLine("SCREEN TIMER:  ->  " + iScreenTimer);
                if (iScreenTimer == 0)
                {
                    //if (Display.iCurrentScreen == 0 || Display.iCurrentScreen == 21)//screen mode > 0 for screen 0
                    if (Display.iCurrentScreen == 21 || (Display.iCurrentScreen == 0 && Display.iScreenMode == 1))//?? why need condition above?? -> screen mode > 0 for screen 0
                    {
                        if (bMainDoorOpen == false && bVaultDoorOpen == false)
                        {
                            CenCom.bTestMode = false;//otherwise won't respond to goto screen requests from SSC
                            CenCom.Reset();
                            Display.GotoScreen(0, 0);
                            RS485.SendCancel();
                            sCode = "";//PD- rev23
                        }
                    }
                    else if (bTestMode == true)
                    {
                        if (sCode == "99")
                        {
                            Display.GotoScreen(21, 1);
                        }
                        else if (sCode == "90")
                        {
                            Display.GotoScreen(21, 2);
                        }
                        else if (sCode == "63")
                        {
                            Display.GotoScreen(21, 3);
                        }
                        else
                        {
                            Display.GotoScreen(21, 0);
                        }

                        EPP.Initialize();//from screen 9
                        CashAcceptor.Disable();//from screen 26
                        bBillRequest = false;
                        EPP.iEncryptionMethod = Convert.ToInt16(FileAccess.sSettings.Substring(FileAccess.sSettings.IndexOf("<ENC_METHOD>") + 12, 1));//from screen 27

                        //PD- rev23 sCode not reset in CenCom.Reset()
                        CenCom.Reset();//PD- resets sCode to put it here; resets card reader (screen 25), etc.
                    }
                    else
                    {
                        if (Display.iCurrentScreen == 2)//PD - Rev 15
                        {
                            RS485.SendCAS();
                            RS485.SendCRS();
                        }
                        RS485.SendCancel();
                    }
                }
            }

            if (bStartup)
            {
                //don't check status until system is ready...
            }
            else
            {
                

                

                if ((Display.iCurrentScreen == 0 && Display.iScreenMode == 0) || Display.iCurrentScreen == 1 || Display.iCurrentScreen == 21 || bForceCheck == true)
                {
                    if (iStatusCheckCount == 0)
                    {
                        if (Printer.bPrinting == false)//if screen = 21... should not need if not in test mode
                        {
                            if (EPP.iType == 2)
                            {
                                EPP.FujitsuReset();
                                Debug.WriteLine("fujitus reset");
                            }
                            else
                            {
                                EPP.CheckStatus();
                            }
                            CardReader.CheckStatus();

                            if (CenCom.bEmMode != 1)
                            {
                                CashAcceptor.bWaitingForResponse = true;//PD- rev21 - otherwise doesn't know if offline
                            }
                            //CashAcceptor.CheckStatus();//always check ca status....sent below
                            RS485.bWaitingForResponse = true;//PD- use to trigger code in rs485 class

                            if (Printer.iType == 3)
                            {
                                //Printer.CheckStatusUSB();
                            }
                            else
                            {
                                Printer.CheckStatus();//PD - 13.1
                            }
                            //Printer.Config();//config instead of check status in case printer loses configuration
                            iStatusCheckCount++;
                        }
                    }
                    else if (iStatusCheckCount == 1)
                    {
                        iStatusCheckCount++;
                    }
                    else if (iStatusCheckCount == 2)
                    {
                        if (RS485.bWaitingForResponse == true && RS485.iStatus > 0)
                        {
                            //RS485.Init();//try to re-open and re-init port
                            RS485.iStatus = 0;
                            //RS485.bWaitingForResponse = false;
                            Debug.WriteLine("RS485 Offline");
                            //RS485.bConfig = false;
                        }
                        if (EPP.bWaitingForResponse == true && EPP.iStatus > 0)
                        {
                            //EPP.Init();//try to re-open and re-init port
                            EPP.iStatus = 0;
                            //EPP.bWaitingForResponse = false;
                            Debug.WriteLine("EPP Offline");
                        }
                        if (CardReader.bWaitingForResponse == true && CardReader.iStatus > 0)
                        {
                            CardReader.iStatus = 0;
                            //CardReader.bWaitingForResponse = false;
                            Debug.WriteLine("CR Offline");
                            RS485.SendCRS();
                        }
                        if (CashAcceptor.bWaitingForResponse == true && CashAcceptor.iStatus > 0)
                        {
                            CashAcceptor.iStatus = 0;
                            //CashAcceptor.bWaitingForResponse = false;
                            Debug.WriteLine("CA Offline");
                            RS485.SendCAS();//won't auto send because status change is in data received handler
                        }
                        if (Printer.iType < 3)
                        {
                            if (Printer.bWaitingForResponse == true && Printer.iStatus > 0)
                            {
                                Printer.iStatus = 0;
                                Printer.bError = false;//so know when offline vs. jam
                                //Printer.bWaitingForResponse = false;
                                Debug.WriteLine("Printer Offline");
                                RS485.SendPRS();
                            }
                            else if (Printer.bWaitingForResponse == true && Printer.bError == true)
                            {
                                Printer.bError = false;//clear jam msg if go offline
                            }
                        }
                        iStatusCheckCount++;
                        bForceCheck = false;
                    }
                    else
                    {
                        iStatusCheckCount++;
                        if (iStatusCheckCount == 10)
                        {
                            iStatusCheckCount = 0;
                        }
                    }

                    Debug.WriteLine("overall status: " + iOverallStatus);
                    Debug.WriteLine("epp status: " + EPP.iStatus);
                    Debug.WriteLine("cr status: " + CardReader.iStatus);
                    Debug.WriteLine("ca status: " + CashAcceptor.iStatus);
                    Debug.WriteLine("ptr status: " + Printer.iStatus);
                    Debug.WriteLine("485 status: " + RS485.iStatus);

                    if (iOverallStatus == 0)//offline->online
                    {
                        if (EPP.iStatus == 1 && RS485.iStatus == 1 && (CashAcceptor.iStatus == 1 || CardReader.iStatus == 1))
                        {
                            iOverallStatus = 1;
                            Display.GotoScreen(1, 0);
                        }
                        
                    }
                    else//online->offline
                    {
                        if (EPP.iStatus == 0 || RS485.iStatus == 0 || (CashAcceptor.iStatus == 0 && CardReader.iStatus == 0))
                        {
                            iOverallStatus = 0;
                            Display.GotoScreen(0, 0);
                        }
                    }
                }

                //Check RS485 Communication and set to 0 after 30 seconds of no monitor
                i485Monitor++;
                //CE - changed from > 15 to > 30 since reboot takes longer
                if (i485Monitor > 30 && RS485.iStatus > 0)
                {
                    RS485.iStatus = 0;
                    //RS485.iMsgSeq = 32;//PD - rev17b3
                    RS485.sDataToSend = "";

                    i485Monitor = 0;

                    if (iLoggingSSC == 1)//PD - rev 16
                    {
                        try
                        {
                            RS485.WriteLogData_TX(Environment.NewLine + "TX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                            RS485.WriteLogData_TX("SSC OFFLINE");
                        }
                        catch { }
                    }

                    if (Display.iCurrentScreen > 0 && Display.iCurrentScreen < 21 && bTestMode == false)
                    {
                        Debug.WriteLine("HERE IT IS!!!!!!!!!!!!!!");
                        CenCom.Reset();
                        Display.GotoScreen(0, 0);
                    }

                    if (CashAcceptor.iStatus == 3 || CashAcceptor.iStatus == 4)//PD- return bill if lose communication before stack
                    {
                        CashAcceptor.Return();//PD - rev21 - rev21 will also return if in escrow too long
                    }
                        
                    //CE - xp relied on launch - Application.Exit();//PD - rev 19
                    Debug.WriteLine("GOODBYE");
                    
                }

                if (bPrintRequest == true)//won't set until already have data to print so check after know customer wants car wash... force check when enter screen 1 for reg. receipt...so bforcecheck should be false by now
                {
                    bPrintRequest = false;

                    //if (Printer.iStatus > 0)PD- rev16 - taken care of w/ prp request
                    //{
                        Debug.WriteLine("trying to print................");
                        //Printer.iStatus = 2;//PD- Need to set this earlier
                        //RS485.SendPRS();
                        if (Printer.iType > 2)
                        {
                            //CEPrinter.PrintUSB(Printer.sReceipt);
                        }
                        else
                        {
                            Printer.Print(Printer.sReceipt);
                        }
                    //}
                    //else
                    //{
                    //    Printer.iStatus = 0;
                    //    RS485.SendPRS();
                    //}
                }
                else if (bTestRequest == true && bForceCheck == false)
                {
                    bTestRequest = false;
                    Debug.WriteLine("trying to test................" + Printer.iStatus);

                    if (Printer.iStatus == 1)//PD- prevent printing while already printing
                    {
                        Display.ShowMessageBox("Printing.  Please wait.", 0);
                        //Printer.iStatus = 2;//don't need to set this for testing
                        Printer.iStatus = 2;//PD- rev. 16 - put back in to prevent multiple prints...
                        if (Printer.iType == 3)
                        {
                            //CEPrinter.TestUSB();
                            //Printer.PrintUSB("\n\n\nhello\n\n\n");
                        }
                        else { Printer.Test(); }
                    }
                    else
                    {
                        if (Printer.iType > 1)
                        {
                            if (Printer.bError)
                            {
                                //sStatus = "Paper Out";
                                sStatus = Printer.sStatus;
                            }
                            else
                            {
                                sStatus = "Printer Offline";
                            }
                        }
                        else
                        {
                            sStatus = "Printer Offline";
                        }
                        //Display.GotoScreen(22, 0);
                        Display.ShowMessageBox(sStatus, 3);
                    }
                }
                else if (Printer.bPrinting)//make sure don't get stuck printing....
                {
                    Debug.WriteLine("PRINT TIMER          :" + iPrintTimer);
                    iPrintTimer++;

                    //if (iPrintTimer > 3)//PD - 13.1
                    //{
                    //    Printer.CheckStatus();
                    //}
                    if (iPrintTimer > iPrinterWait)//PD - 13.1 (change to 5 from 10)
                    {
                        Printer.bPrinting = false;

                        if (bTestMode)
                        {
                            Printer.iStatus = 1;//PD- rev 16
                            Display.HideMessageBox();
                        }
                        else
                        {
                            //if (iPrintTimer > 30)
                            //{//set to 30 in printer class when print completion is detected.
                            //    Printer.iStatus = 1;
                            //}
                            //else
                            //{
                            //    Printer.iStatus = 0;
                            //}
                            Printer.iStatus = 1;//PD-just set to 1... let checkstatus change to offline...
                            RS485.SendPRS();

                            if (Display.iCurrentScreen == 20 && Display.iScreenMode == 15)
                            {
                                bForceScreenChange = false;

                                if (CenCom.bSpanish)
                                {
                                    if (iBrand == 2)
                                    {
                                        Display.screen0.SetText(Display.screen0.lMsg, "Por favor tome su recibo.\n\nGracias por escoger Thrifty.");
                                    }
                                    else
                                    {
                                        Display.screen0.SetText(Display.screen0.lMsg, "Por favor tome su recibo.\n\nGracias por escoger ARCO.");
                                    }
                                }
                                else
                                {
                                    if (iBrand == 2)
                                    {
                                        Display.screen0.SetText(Display.screen0.lMsg, "Please take your receipt.\n\nThank you for choosing Thrifty.");
                                    }
                                    else
                                    {
                                        Display.screen0.SetText(Display.screen0.lMsg, "Please take your receipt.\n\nThank you for choosing ARCO.");
                                    }
                                }
                                Display.GotoScreen(14, 20);//set mode to 1 to detect this exact screen when timeout occurs...
                                //iScreenTimer = 10;PD-set in GOTOSCREEN method
                            }
                           
                        }
                    }
                }

                if (bStatusRequest == true && bForceCheck == false)
                {
                    bStatusRequest = false;

                    sStatus = "";
                    //if (bAlarmOn) { sStatus = sStatus + "Alarm On\n"; }
                    if (bMainDoorOpen) { sStatus = sStatus + "Door Open\n"; }
                    if (bVaultDoorOpen) { sStatus = sStatus + "Vault Door Open\n"; }
                    if (CardReader.iStatus == 0) { sStatus = sStatus + "Card Reader Offline\n"; }
                    if (CashAcceptor.iStatus == 0) { sStatus = sStatus + "Cash Acceptor Offline\n"; }
                    else if (CashAcceptor.iStatus == 11) { sStatus = sStatus + "Bill Jammed\n"; }
                    else if (CashAcceptor.iStatus == 12) { sStatus = sStatus + "Cassette Full\n"; }
                    else if (CashAcceptor.iStatus == 13) { sStatus = sStatus + "Cassette Removed\n"; }
                    else if (CashAcceptor.iStatus == -2) { sStatus = sStatus + "Cash Acceptor Failure\n"; }
                    Debug.WriteLine("HI PSTST-"+Printer.iStatus);
                    if (Printer.iStatus == 0)
                    { 
                        if (Printer.iType > 1)
                        {
                            Debug.WriteLine("PRINTER STATUS 1:" + Printer.sStatus);
                            if (Printer.bError)
                            {
                                //sStatus = sStatus + "Paper Out\n";
                                sStatus = sStatus + Printer.sStatus + "\n";
                            }
                            else
                            {
                                sStatus = sStatus + "Printer Offline\n";
                            }
                        }
                        else
                        {
                            sStatus = sStatus + "Printer Offline\n";
                        }
                    }
                    else if (Printer.iStatus == 1)
                    {
                        Debug.WriteLine("PRINTER STATUS 2:" + Printer.sStatus);
                        if (Printer.iType > 1 && Printer.sStatus != "")
                        {
                            sStatus = sStatus + Printer.sStatus + "\n";
                        }
                    }
                    if (RS485.iStatus == 0) { sStatus = sStatus + "PIC Offline\n"; }
                    if (sStatus == "") { sStatus = "System OK"; }
                    //Display.GotoScreen(22, 0);
                    Display.ShowMessageBox(sStatus, 3);
                }

                //if (Display.iCurrentScreen == 1 && CardReader.bCardInserted == true && CardReader.bCardLock == false)
                //{
                //    CardReader.iCardInsertedTimer++;
                //    if (CardReader.iCardInsertedTimer == 5)
                //    {
                //        CardReader.bCardLock = true;
                //    }
                //}

                if (bBillRequest == true)//need to constantly send...assume on screen 26, 3, ... where bills are accepted
                {
                    CashAcceptor.Enable();
                }
                else
                {
                    if (CashAcceptor.iStatus == 3 || CashAcceptor.iStatus == 4)
                    {
                        CashAcceptor.Return();//CA PROBLEM IN OREGON - CA STACKS BILL IF DISABLED WHILE ACCEPTING
                    }
                    else
                    {
                        CashAcceptor.Disable();
                    }
                }
            }
        }

        public static void ImmediateCheck()
        {
            if (iStatusCheckCount > 1)//only if didn't just check..
            {
                bForceCheck = true;
                iStatusCheckCount = 0;
            }
        }

        public static void Beep(int iTimes)
        {
            iBeep = iTimes;
        }

        public static void BeepOn()
        {
            if (EPP.PortEPP.IsOpen)
            {
                if (sOS == "XP")
                {
                    EPP.PortEPP.DtrEnable = true;
                }
                else
                {
                    EPP.PortEPP.RtsEnable = true;
                }
            }
        }

        public static void BeepOff()
        {
            if (EPP.PortEPP.IsOpen)
            {
                if (sOS == "XP")
                {
                    EPP.PortEPP.DtrEnable = false;
                }
                else
                {
                    EPP.PortEPP.RtsEnable = false;
                }
            }
        }

        public static void AlarmOn()
        {
            bAlarmOn = true;
            bAlarmEnabled = false;

            if (sOS == "XP" && CashAcceptor.PortCA.IsOpen)
            {
                CashAcceptor.PortCA.DtrEnable = true;
            }
            else if (sOS == "CE" && Printer.PortPTR.IsOpen)
            {
                Printer.PortPTR.RtsEnable = true;
            }
                
            RS485.SendGNS();

            if (Display.iCurrentScreen == 21)
            {
                Display.screen0.SetText(Display.screen0.lMsgBottom, "Select action, press CANCEL when done." + "\n" + "PIC ID: " + CenCom.sPICID + "             " + AlarmStatus());
            }
        }

        public static void AlarmOff()
        {
            bAlarmOn = false;

            if (sOS == "XP" && CashAcceptor.PortCA.IsOpen)
            {
                CashAcceptor.PortCA.DtrEnable = false;
            }
            else if (sOS == "CE" && Printer.PortPTR.IsOpen)
            {
                Printer.PortPTR.RtsEnable = false;
            }

            RS485.SendGNS();
        }

        public static string AlarmStatus()
        {
            if (bAlarmOn)
            {
                return "Alarm on.";
            }
            else
            {
                if (bAlarmEnabled)
                {
                    return "Alarm enabled.";
                }
                else
                {
                    return "Alarm disabled.";
                }
            }
        }

        public static bool IsMainDoorClosed()
        {
            return true;
            /*
            if (EPP.PortEPP.IsOpen)
            {
                if (sOS == "XP")
                {
                    return EPP.PortEPP.DsrHolding;
                }
                else
                {
                    return EPP.PortEPP.CtsHolding;
                }
            }
            else
            {
                return false;
            }
            */
        }

        public static bool IsVaultDoorClosed()
        {
            //override
            return true;
          /*  
            if (sos == "xp" && cashacceptor.portca.isopen)
            {
                return cashacceptor.portca.dsrholding;
            }
            else if (sos == "ce" && printer.portptr.isopen)
            {
                return printer.portptr.ctsholding;
            }
            else
            {
                return false;
            }
            */
        }

        public static void MyUpdateStatus()
        {
            Debug.WriteLine("Update Status. EPP = " + iEPPStatus + "/" + EPP.iStatus + " CR = " + iCRStatus + " / " + CardReader.iStatus + " CA = " + iCAStatus + "/" + CashAcceptor.iStatus + " PR = " + iPRStatus + "/" + Printer.iStatus);

            if (CenCom.inputType == 1)
            {
                if (iEPPStatus != EPP.iStatus)
                {
                    iEPPStatus = EPP.iStatus;
                    RS485.MySendStatus(1, iEPPStatus);
                }
            }
            //Force touchscreen status update to 1
            if (CenCom.inputType == 2)
            {
                RS485.MySendStatus(1, 1);
            }
            if (iCRStatus != CardReader.iStatus)
            {
                iCRStatus = CardReader.iStatus;
                RS485.MySendStatus(3, iCRStatus);
            }
            if (iCAStatus != CashAcceptor.iStatus)
            {
                iCAStatus = CashAcceptor.iStatus;
                RS485.MySendStatus(4, iCAStatus);
            }
            if (iPRStatus != Printer.iStatus)
            {
                iPRStatus = Printer.iStatus;
                RS485.MySendStatus(5, iPRStatus);
            }
        }

        public static bool MySetString(int iIndex, string sNew)
        {
            Debug.WriteLine("Set String " + iIndex + " = " + sNew);

            sBox[iIndex] = sNew;

            return true;
        }

        public static string MyGetString(int iIndex)
        {
            Debug.WriteLine("Get String " + iIndex);

            return sBox[iIndex];
        }

        public static void ProcessKey(int iKey)
        {
            string sStars = "************";

            Debug.WriteLine("Screen " + Display.iCurrentScreen);

            Beep(1);

            if (bTestMode)//RESET TIMER NO MATTER WHAT'S PRESSED ON THE TEST SCREENS
            {
                iScreenTimer = 60;
            }

            if (Display.bMsgBoxShowing || (bForceCheck && bTestMode))//PD- check for bForceCheck prohibits user interaction for too long on screen 1 and car wash selection
            {
                //don't process if msg box is showing
                //Display.ShowMessageBox("HELLLOOOO", 10);
            }
            else if (iKey == 12 && bTestMode == false)
            {
                if (Display.iCurrentScreen == 2)
                {
                    if (bSpanish)
                    {
                        bSpanish = false;
                        if (EPP.iType == 2)
                        {
                            Display.screen0.SetText(Display.screen0.lPrompt, "Enter pump number and\npress OK");
                        }
                        else
                        {
                            Display.screen0.SetText(Display.screen0.lPrompt, "Enter pump number and\npress ENTER");
                        }
                    }
                    else
                    {
                        bSpanish = true;
                        if (EPP.iType == 2)
                        {
                            Display.screen0.SetText(Display.screen0.lPrompt, "Marcar n" + Convert.ToChar(250) + "mero de bomba y\npresionar OK");
                        }
                        else
                        {
                            Display.screen0.SetText(Display.screen0.lPrompt, "Marcar n" + Convert.ToChar(250) + "mero de bomba y\npresionar ENTER");
                        }
                    }
                }
                else if (Display.iCurrentScreen == 8 && Display.iScreenMode == 2)
                {
                    if (bSpanish)
                    {
                        bSpanish = false;
                        Display.screen0.SetText(Display.screen0.lPromptChoices, "Would you like to pump gas or get receipt?");
                        Display.screen0.SetText(Display.screen0.lChoices, "Press 1 to Pump Gas.\nPress 2 to Get Receipt.");
                        Display.screen0.SetText(Display.screen0.lPromptBottom, "Pump " + CenCom.sPump + " selected.");
                    }
                    else
                    {
                        bSpanish = true;
                        Display.screen0.SetText(Display.screen0.lPromptChoices, "" + Convert.ToChar(191) + "Desearia llenar tanque o tomar su recibo?");
                        Display.screen0.SetText(Display.screen0.lChoices, "Presionar 1 para llenar tanque.\nPresionar 2 para su recibo.");
                        Display.screen0.SetText(Display.screen0.lPromptBottom, "Bomba " + CenCom.sPump + " seleccionada.");
                    }
                }
                else
                {
                    RS485.SendEspanol();
                }
            }

            #region screen 0 or 2

            else if (Display.iCurrentScreen == 0 || Display.iCurrentScreen == 2)
            {
                if (Display.iCurrentScreen == 0)
                {
                    if (Display.iScreenMode == 0)
                    {
                        CenCom.iScreenTimer = 10;
                    }
                    else if (Display.iScreenMode == 1)
                    {
                        CenCom.iScreenTimer = 60;
                    }
                }
                else//screen = 2 Enter pump number with s input and hidden logo
                {
                    CenCom.iScreenTimer = 10;
                }

                Debug.WriteLine("Max input: " + Display.iMaxInput);

                if (iKey >= 0 && iKey <= 9)
                {
                    if (sInput.Length < Display.iMaxInput)
                    {
                        sInput = sInput + Convert.ToString(iKey);

                        if (Display.iCurrentScreen == 0)
                        {
                            if (Display.iScreenMode == 1 || Display.iScreenMode == 7)
                            {
                                Display.screen0.SetText(Display.screen0.lInput, sStars.Substring(0,sInput.Length));
                            }
                            else
                            {
                                Display.screen0.SetText(Display.screen0.lInput, sInput);
                            }
                        }
                        else
                        {
                            Display.screen0.SetText(Display.screen0.lInput2, sInput);//CE - for screen 2
                        }
                    }
                    else if (Display.iMaxInput == 1)//pic id
                    {
                        sInput = Convert.ToString(iKey);
                        Display.screen0.SetText(Display.screen0.lInput, sInput);
                    }
                    else if (sInput.Length >= Display.iMaxInput)
                    {
                        Beep(1);

                        if (iMsgBoxTimer == 0)//don't show while another is showing
                        {
                            if (bSpanish)
                            {
                                Display.ShowMessageBox("Demasiados d" + Convert.ToChar(237) + "gitos.", 3);
                            }
                            else
                            {
                                Display.ShowMessageBox("Entry too long.", 3);
                            }
                        }
                    }
                }
                else if (iKey == 15)
                {

                    //if (sInput != "")
                    //{
                        if (sInput.Length < Display.iMinInput)
                        {
                            Beep(1);

                            if (iMsgBoxTimer == 0)
                            {
                                if (bSpanish)
                                {
                                    Display.ShowMessageBox("Faltan d" + Convert.ToChar(237) + "gitos.", 3);
                                }
                                else
                                {
                                    Display.ShowMessageBox("Entry too short.", 3);
                                }
                            }
                        }
                        else if (Display.iCurrentScreen == 2 && sInput != "" && sInput != "99" && sInput != "90" && sInput != "71" && sInput != "63")
                        {
                            Debug.WriteLine("auth pump");
                            sPump = sInput;

                            if (bPreSwipe == false)
                            {
                                if (CenCom.bSpanish)
                                {
                                    Display.screen0.SetText(Display.screen0.lPromptChoices, "\n" + Convert.ToChar(191)+"Desearia llenar tanque o tomar su recibo?");
                                    Display.screen0.SetText(Display.screen0.lChoices, "\nPresionar 1 para llenar tanque.\nPresionar 2 para su recibo.");
                                }
                                else
                                {
                                    Display.screen0.SetText(Display.screen0.lPromptChoices, "\nWould you like to pump gas or get receipt?");
                                    Display.screen0.SetText(Display.screen0.lChoices, "\nPress 1 to Pump Gas.\nPress 2 to Get Receipt.");
                                }

                                Display.GotoScreen(8, 2);
                            }
                            else
                            {
                                RS485.SendInput(sInput);
                            }
                        }
                        else if (Display.iScreenMode == 0 && sInput == "99")//screen 0 or 2
                        {
                            sCode = "99";
                            Display.GotoScreen(0, 1);
                        }
                        else if (Display.iScreenMode == 0 && sInput == "90")//screen 0 or 2
                        {
                            sCode = "90";
                            Display.GotoScreen(0, 1);
                        }
                        else if (Display.iScreenMode == 0 && sInput == "71")//screen 0 or 2
                        {
                            sCode = "71";
                            Display.GotoScreen(0, 1);
                        }
                        else if (Display.iScreenMode == 0 && sInput == "63")//screen 0 or 2
                        {
                            sCode = "63";
                            Display.GotoScreen(0, 1);
                        }
                        else if (Display.iScreenMode == 1 && sCode == "99" && sInput == "4841")
                        {
                            bTestMode = true;
                            Display.GotoScreen(21, 1);
                        }
                        else if (Display.iScreenMode == 1 && sCode == "90" && sInput == "4534")
                        {
                            bTestMode = true;
                            Display.GotoScreen(21, 2);
                        }
                        else if (Display.iScreenMode == 1 && sCode == "63" && sInput == "6554")
                        {
                            bTestMode = true;
                            Display.GotoScreen(21, 3);
                        }
                        else if (Display.iScreenMode == 1 && sCode == "71" && sInput == "4565")
                        {
                            bTestMode = true;
                            Display.GotoScreen(21, 0);
                        }
                        else if (Display.iScreenMode == 2)
                        {
                            Debug.WriteLine("send zip code");
                            RS485.SendInput(sInput);
                        }
                        else if (Display.iScreenMode == 3)
                        {
                            Debug.WriteLine("send odometer");
                            RS485.SendInput(sInput);
                        }
                        else if (Display.iScreenMode == 5)
                        {
                            Debug.WriteLine("send driver #");
                            RS485.SendInput(sInput);
                        }
                        else if (Display.iScreenMode == 6)
                        {
                            Debug.WriteLine("send vehicle #");
                            RS485.SendInput(sInput);
                        }
                        else if (Display.iScreenMode == 7)
                        {
                            Debug.WriteLine("send id #");
                            RS485.SendInput(sInput);
                        }
                        else if (Display.iScreenMode == 4)
                        {
                            Debug.WriteLine("set pic id");
                            sPICID = sInput;
                            iPICID = Convert.ToInt16(sPICID);

                            iCurrentSetting = Convert.ToInt16(FileAccess.sSettings.Substring(FileAccess.sSettings.IndexOf("<PICID>") + 7, 1));
                            if (iCurrentSetting != iPICID)
                            {
                                FileAccess.sSettings = FileAccess.sSettings.Replace("<PICID>" + Convert.ToString(iCurrentSetting), "<PICID>" + sPICID);
                                if (sOS == "CE")
                                {
                                    FileAccessCE.WriteFile("\\SDCard\\SPT\\settings.txt", FileAccess.sSettings);
                                }
                                else
                                {
                                    File.WriteAllText("settings.txt", FileAccess.sSettings);
                                }
                                Display.GotoScreen(21, 1);
                                Display.ShowMessageBox("PIC ID saved", 3);
                            }
                            else
                            {
                                Display.GotoScreen(21, 1);
                            }
                        }
                        else
                        {
                            Debug.WriteLine("clear code");
                            sInput = "";
                            Display.screen0.SetText(Display.screen0.lInput, sInput);
                        }
                    //}
                }
                else if (iKey == 14)
                {
                    Debug.WriteLine("clear" + "sInput" + sInput + "length" + sInput.Length);
                    if (sInput.Length > 0 && Display.iMaxInput > 1)//don't clear pic id
                    {
                        sInput = sInput.Substring(0, sInput.Length - 1);
                        if (Display.iCurrentScreen == 0)
                        {
                            if (Display.iScreenMode == 1 || Display.iScreenMode == 7)
                            {
                                Display.screen0.SetText(Display.screen0.lInput, sStars.Substring(0, sInput.Length));
                            }
                            else
                            {
                                Display.screen0.SetText(Display.screen0.lInput, sInput);
                            }

                            if (Display.iScreenMode == 0)
                            {
                                CenCom.iScreenTimer = 10;
                            }
                            else if (Display.iScreenMode == 1)
                            {
                                CenCom.iScreenTimer = 60;
                            }
                        }
                        else
                        {
                            CenCom.iScreenTimer = 10;
                            Display.screen0.SetText(Display.screen0.lInput2, sInput);//CE - for screen 2
                        }
                    }
                }
                else if (iKey == 13)
                {
                    if (Display.iCurrentScreen == 0 && Display.iScreenMode < 4)
                    {
                        //CenCom.iScreenTimer = 0;//PD- handled by gotoscreen
                        ProcessCancel();
                    }
                    else if (Display.iCurrentScreen == 0 && Display.iScreenMode == 4)//pic id
                    {
                        //CenCom.iScreenTimer = 60;//PD- handled by gotoscreen
                        Display.GotoScreen(21, 1);
                    }
                    else//screen 2 pump entry
                    {
                        //CenCom.iScreenTimer = 0;//PD- handled by gotoscreen
                        
                        if (bSpanish != bScreen1Spanish)
                        {
                            RS485.SendEspanol();
                        }
                        RS485.SendCAS();
                        RS485.SendCRS();
                        RS485.SendCancel();
                    }
                }
            }
            #endregion

            #region screen 9
            else if (Display.iCurrentScreen == 9)
            {
                Debug.WriteLine("Processing key on screen 9: " + iKey);
                if (iKey == 42)
                {
                    if (sPINEntry.Length >= Display.iMaxInput)
                    {
                        Beep(1);

                        if (iMsgBoxTimer == 0)//don't show while another is showing
                        {
                            if (bSpanish)
                            {
                                Display.ShowMessageBox("Demasiados d" + Convert.ToChar(237) + "gitos.", 3);
                            }
                            else
                            {
                                Display.ShowMessageBox("Entry too long.", 3);
                            }
                        }
                    }
                    else
                    {
                        sPINEntry = sPINEntry + "*";
                    }
                    Display.screen0.SetText(Display.screen0.lInput, sPINEntry); //CE
                }
                else if (iKey == 15)
                {

                    Debug.WriteLine("Enter pressed to general PIN block");
                    
                    //****if false, please try again......  make sure have valid acct num before entering this screen.

                    // don't process any keys until pin block is parsed...

                    //EPP.Initialize();Don't do this until after process EPB, otherwise will delete it...

                    // text field will populate w/ result after pin block is parsed.
                    if (sPINEntry.Length < Display.iMinInput)
                    {
                        Beep(1);

                        if (iMsgBoxTimer == 0)
                        {
                            if (bSpanish)
                            {
                                Display.ShowMessageBox("Faltan d" + Convert.ToChar(237) + "gitos.", 3);
                            }
                            else
                            {
                                Display.ShowMessageBox("Entry too short.", 3);
                            }
                        }
                        //EPP.EnableEncryption(EPP.iEncryptionMethod);
                    }
                    else if (bTestMode == false)//PD-wait 'til get feedback from 0x91 cmd - otherwise rev 13 fails in release
                    {
                        //bGeneratePINBlock = true;
                        //EPP.bGeneratedPINBlock = EPP.GeneratePINBlock(EPP.iEncryptionMethod, CardReader.sPAN);
                    }
                    else//PD-wait 'til get feedback from 0x91 cmd - otherwise rev 13 fails in release
                    {
                        //bGeneratePINBlock = true;
                        //EPP.bGeneratedPINBlock = EPP.GeneratePINBlock(EPP.iEncryptionMethod, FileAccess.GetTestPAN());
                        //Display.GotoScreen(27, 0);
                    }
                }
                else if (iKey == 14)
                {
                    Debug.WriteLine("clear" + "sPINEntry" + sPINEntry + "length" + sPINEntry.Length);
                    if (sPINEntry.Length > 0)
                    {
                        sPINEntry = sPINEntry.Substring(0, sPINEntry.Length - 1);
                        Display.screen0.SetText(Display.screen0.lInput, sPINEntry);
                    }
                }
                else if (iKey == 13)
                {
                    EPP.Initialize();

                    if (bTestMode == false)
                    {
                        RS485.SendCancel();
                    }
                    else
                    {

                        Display.GotoScreen(27, 0);
                    }
                }
            }
            #endregion

            else if (Display.iCurrentScreen == 1)
            {
                //if (iKey == 1)
                //{
                //    RS485.SendF1();
                //}
                //if (iKey == 2)
                //{
                //    RS485.SendF2();
                //}

                if (iKey >= 0 && iKey <= 9)
                {
                    bPreInput = true;
                    sInput = Convert.ToString(iKey);
                    Display.screen0.SetText(Display.screen0.lInput2, sInput);

                    if (CenCom.bSpanish)
                    {
                        if (EPP.iType == 2)
                        {
                            Display.screen0.SetText(Display.screen0.lPrompt, "Marcar n" + Convert.ToChar(250) + "mero de bomba y\npresionar OK");
                        }
                        else
                        {
                            Display.screen0.SetText(Display.screen0.lPrompt, "Marcar n" + Convert.ToChar(250) + "mero de bomba y\npresionar ENTER");
                        }
                    }
                    else
                    {
                        if (EPP.iType == 2)
                        {
                            Display.screen0.SetText(Display.screen0.lPrompt, "Enter pump number and\npress OK");
                        }
                        else
                        {
                            Display.screen0.SetText(Display.screen0.lPrompt, "Enter pump number and\npress ENTER");
                        }
                    }
                    Display.GotoScreen(2, 0);
                }
            }
            else if (Display.iCurrentScreen == 5 || Display.iCurrentScreen == 4 || Display.iCurrentScreen == 3)
            {
                if (iKey == 13)
                {
                    //iScreenTimer = 0;//PD- reset by gotoscreen...

                    bCardRequest = false;

                    if (CashAcceptor.iStatus != 3 && CashAcceptor.iStatus != 4)//don't allow while accepting
                    {
                        CashAcceptor.Disable();
                        RS485.SendCancel();
                        CashAcceptor.iBillTotal = 0;
                        CenCom.bBillRequest = false;
                    }
                }
            }
            else if (Display.iCurrentScreen == 7)
            {
                if (iKey == 13)
                {
                    //Show Msg Box with Question if this is the first time
                    if (!requestCancel && CashAcceptor.iStatus != 3 && CashAcceptor.iStatus != 4)
                    {
                        CashAcceptor.Disable();
                        CashAcceptor.iBillTotal = 0;
                        CenCom.bBillRequest = false;
                        requestCancel = true;
                        Display.screen0.SetText(Display.screen0.lPromptTop, "Are you sure you want to Cancel?\nPress 1 to Cancel. Press 2 to Continue.");
                        Display.screen0.SetText(Display.screen0.lPromptBottom, "");
                    }
                    //else if (CashAcceptor.iStatus != 3 && CashAcceptor.iStatus != 4)//don't allow while accepting
                    //{
                    //    CashAcceptor.Disable();
                    //    RS485.SendCancel();
                    //    CashAcceptor.iBillTotal = 0;
                    //    CenCom.Reset();
                    //    CenCom.bBillRequest = false;
                    //}
                }
                if (iKey == 1 && requestCancel)
                {
                    //Execute Cancel
                    CashAcceptor.Disable();
                    RS485.SendCancel();//Send twice because server needs confirmation
                    RS485.SendCancel();
                    Display.ShowMessageBox("Your transaction has been canceled.\n\nPlease retrieve your change from\ninside the store.", 5);
                    CashAcceptor.iBillTotal = 0;                   
                    CenCom.bBillRequest = false;
                    requestCancel = false;
                    CenCom.Reset();
                }
                else if (iKey == 2 && requestCancel)
                {
                    //Continue
                    CashAcceptor.Enable();
                    CashAcceptor.iBillVal = 0;
                    CenCom.bBillRequest = true;
                    Display.screen0.SetText(Display.screen0.lPromptTop, "Insert bills and press ENTER after last bill.\nTotal dollar amount accepted:");
                    Display.screen0.SetText(Display.screen0.lPromptBottom, "");
                    requestCancel = false;
                }

                else if (iKey == 15)
                {
                    if (CashAcceptor.iStatus != 3 && CashAcceptor.iStatus != 4)//don't allow while accepting
                    {
                        CashAcceptor.Disable();
                        RS485.SendEnter();
                        CenCom.Reset();
                        CashAcceptor.iBillTotal = 0;
                        CenCom.bBillRequest = false;
                    }
                }
            }
            else if (Display.iCurrentScreen == 8)
            {
                if (Display.iScreenMode == 3)//reverse response order for card type choice (debit or credit)
                {
                    if (iKey == 1)
                    {
                        RS485.QuestionResponse1(false);
                    }
                    else if (iKey == 2)
                    {
                        RS485.QuestionResponse1(true);
                    }
                    else if (iKey == 13)
                    {
                        RS485.SendCancel();
                    }
                }
                else if (Display.iScreenMode == 2)
                {
                    if (iKey == 1)
                    {
                        if (bSpanish != bScreen1Spanish)
                        {
                            RS485.SendEspanol();
                        }
                        RS485.SendCAS();//PD - rev15
                        RS485.SendCRS();//PD - rev15 - report bill jam, etc.... 
                        RS485.PumpGas(sPump);
                        //int epptester = Convert.ToInt16((sPump), 2);
                        RS485.MyStartTransaction(Convert.ToInt16(sPump), 1);
                    }
                    else if (iKey == 2)
                    {
                        if (bSpanish != bScreen1Spanish)
                        {
                            RS485.SendEspanol();
                        }
                        //RS485.GetReceipt(sPump);
                        RS485.GetReceiptPart1();
                    }
                    else if (iKey == 13)
                    {
                        if (bSpanish != bScreen1Spanish)
                        {
                            RS485.SendEspanol();
                        }
                        RS485.SendCancel();
                        ProcessCancel();
                    }
                } 
                else if (Display.iScreenMode == 1)
                {
                    if (iKey == 1 && iNumWashes > 0)
                    {
                        RS485.SendF1();
                    }
                    else if (iKey == 2 && iNumWashes > 1)
                    {
                        RS485.SendF2();
                    }
                    else if (iKey == 3 && iNumWashes > 2)
                    {
                        RS485.SendF3();
                    }
                    else if (iKey == 4 && iNumWashes > 3)
                    {
                        RS485.SendF4();
                    }
                    else if (iKey == 13)
                    {
                        RS485.SendCancel();
                    }
                }
                else {
                    if (iKey == 1)
                    {
                        RS485.QuestionResponse1(true);
                    }
                    else if (iKey == 2)
                    {
                        RS485.QuestionResponse1(false);
                    }
                    else if (iKey == 13)
                    {
                        RS485.SendCancel();
                    }
                }
            }
            else if (Display.iCurrentScreen == 14)
            {
                if (iKey == 13)
                {
                    bForceScreenChange = true;
                    RS485.SendCancel();
                }
            }
            else if (Display.iCurrentScreen == 20)
            {
                if (iKey == 13)
                {
                    if (CenCom.bTestMode == true)//PD-when would you be on screen 20 during test mode
                    {
                        //bTestMode = false;PD-don't do this unless cancel from screen 21...
                        ////CenCom.iScreenTimer = 0;//PD- reset by gotoscreen
                        //ProcessCancel();PD-why is this here????
                        ////Display.GotoScreen(21, 0);//PD- SHOULDN'T NEED THIS... MODE MAY NOT BE 0!!!!
                    }
                    else
                    {
                        if (CashAcceptor.iStatus != 3 && CashAcceptor.iStatus != 4)//PD- don't allow cancel when processing bills and showing $# accepted one moment please screen.
                        {
                            RS485.SendCancel();
                        }
                    }
                }
            }
            else if (Display.iCurrentScreen == 21)
            {
                //CenCom.iScreenTimer = 60;//PD-use bTestMode above instead...reset with any key press

                if (iKey == 13)
                {
                    if (bMainDoorOpen || bVaultDoorOpen)
                    //if (bVaultDoorOpen)
                    {
                        Display.ShowMessageBox("Close door please", 3);
                        Beep(1);
                    }
                    else
                    {
                        bTestMode = false;
                        //CenCom.iScreenTimer = 0;//PD- reset by gotoscreen
                        ProcessCancel();
                    }
                }
                else if (iKey == 1 && Display.iScreenMode > 0)
                {
                    if (bAlarmOn)
                    {
                        AlarmOff();
                    }
                        bAlarmEnabled = false;
                        Display.ShowMessageBox("Alarm disabled", 3);
                        Display.screen0.SetText(Display.screen0.lMsgBottom, "Select action, press CANCEL when done." + "\n" + "PIC ID: " + CenCom.sPICID + "             " + AlarmStatus());
                }
                else if (iKey == 1 && Display.iScreenMode == 0)
                {
                    if (Printer.iType > 1)
                    {
                        if (bAlarmOn)
                        {
                            AlarmOff();
                        }
                        bAlarmEnabled = false;
                        Display.ShowMessageBox("Alarm disabled", 3);
                        Display.screen0.SetText(Display.screen0.lMsgBottom, "Select action, press CANCEL when done." + "\n" + "PIC ID: " + CenCom.sPICID + "             " + AlarmStatus());
                    }
                    else
                    {
                        CenCom.ImmediateCheck();
                        CenCom.bStatusRequest = true;
                    }
                }
                else if (iKey == 2 && (Display.iScreenMode > 0 && Display.iScreenMode < 3))
                {
                    CenCom.ImmediateCheck();
                    CenCom.bStatusRequest = true;
                }
                else if (iKey == 2 && Display.iScreenMode == 3)
                {
                    if (CenCom.sOS == "CE")
                    {
                        Process pNew = null;
                        Process pCurrent = null;
                        try
                        {
                            if (File.Exists("\\HardDisk\\_INSTALL\\RunMe.bat"))
                            {
                                pNew = new Process();
                                pCurrent = new Process();

                                pCurrent = Process.GetCurrentProcess();

                                pNew.StartInfo.WorkingDirectory = "\\HardDisk\\_INSTALL";
                                pNew.StartInfo.FileName = "\\HardDisk\\_INSTALL\\RunMe.bat";

                                pNew.Start();
                                //pNew.WaitForExit();
                                pCurrent.Kill();
                            }
                            else
                            {
                                Display.ShowMessageBox("Update Error", 3);
                            }
                        }
                        catch
                        {
                            Display.ShowMessageBox("Error", 3);
                        }
                    }
                    else
                    {
                        Process p = null;
                        try
                        {
                            string targetDir;
                            targetDir = string.Format(@"D:\_INSTALL");
                            p = new Process();
                            p.StartInfo.WorkingDirectory = targetDir;
                            p.StartInfo.FileName = "RunMe.bat";
                            p.Start();
                            p.WaitForExit();
                        }
                        catch
                        {
                            Display.ShowMessageBox("Error", 3);
                        }
                    }
                }
                else if (iKey == 5 && Display.iScreenMode == 3)
                {
                    Process pNew = null;
                    try
                    {
                        if (File.Exists("\\HardDisk\\_INSTALL\\RunMe.bat"))
                        {
                            pNew = new Process();

                            pNew.StartInfo.WorkingDirectory = "\\HardDisk\\_INSTALL";
                            pNew.StartInfo.FileName = "\\HardDisk\\_INSTALL\\RunMe.bat";

                            pNew.Start();
                        }
                        else
                        {
                            Display.ShowMessageBox("Update Error", 3);
                        }
                    }
                    catch
                    {
                        Display.ShowMessageBox("Error", 3);
                    }
                }
                else if (iKey == 6 && Display.iScreenMode == 3)
                {
                    Process pNew = null;
                    try
                    {
                        pNew = new Process();

                        //pNew.StartInfo.WorkingDirectory = "\\HardDisk\\_INSTALL";
                        pNew.StartInfo.FileName = "explorer.exe";

                        pNew.Start();
                    }
                    catch
                    {
                        Display.ShowMessageBox("Error", 3);
                    }
                }
                else if (iKey == 7 && Display.iScreenMode == 3)
                {
                    Process pCurrent = null;
                    try
                    {
                        pCurrent = new Process();

                        pCurrent = Process.GetCurrentProcess();

                        pCurrent.Kill();
                    }
                    catch
                    {
                        Display.ShowMessageBox("Error", 3);
                    }
                }
                else if (iKey == 2 && Display.iScreenMode == 0)
                {
                    if (Printer.iType > 1)
                    {
                        CenCom.ImmediateCheck();
                        CenCom.bStatusRequest = true;
                    }
                    else
                    {
                        CenCom.ImmediateCheck();
                        CenCom.bTestRequest = true;
                    }
                }
                else if (iKey == 3 && Display.iScreenMode==1)
                {
                    Debug.WriteLine("Enter PIC ID");
                    Display.GotoScreen(0, 4);
                }
                else if (iKey == 3 && Display.iScreenMode == 2)
                {
                    Debug.WriteLine("Request Armored Report");
                    RS485.RequestArmoredReport();
                }
                else if (iKey == 3 && Display.iScreenMode == 3 && bLogMaster)
                {
                    try
                    {
                        if (sOS == "CE")
                        {
                            FileAccessCE.CopyLogs();
                        }
                        else
                        {
                            FileAccessXP.CopyLogs();
                        }
                        Display.ShowMessageBox("Logs copied", 3);
                    }
                    catch
                    {
                        Display.ShowMessageBox("Error", 3);
                    }
                }
                else if (iKey == 3 && Printer.iType > 1)
                {
                    CenCom.ImmediateCheck();
                    CenCom.bTestRequest = true;
                }
                else if ((iKey == 4 && Display.iScreenMode > 0))
                {
                    CenCom.ImmediateCheck();
                    CenCom.bTestRequest = true;
                }
                else if (iKey == 5 && Display.iScreenMode == 1)
                {
                    if (CardReader.iStatus > 0)
                    {
                        Display.GotoScreen(25, 0);
                    }
                    else
                    {
                        sStatus = "Card Reader Offline";
                        //Display.GotoScreen(22, 0);
                        Display.ShowMessageBox(sStatus, 3);
                    }
                }
                else if (iKey == 6 && Display.iScreenMode == 1)
                {
                    //PD- rev19b3
                    //if (CashAcceptor.iStatus == 0) { Display.ShowMessageBox("Cash Acceptor Offline", 3); }
                    //else if (CashAcceptor.iStatus == 11) { Display.ShowMessageBox("Bill Jammed", 3); }
                    //else if (CashAcceptor.iStatus == 12) { Display.ShowMessageBox("Cassette Full", 3); }
                    //else if (CashAcceptor.iStatus == 13) { Display.ShowMessageBox("Cassette Removed", 3); }
                    //else if (CashAcceptor.iStatus == -2) { Display.ShowMessageBox("Cash Acceptor Failure", 3); }
                    //else 
                    //{
                    bBillRequest = true;
                    //PD- rev20
                    //CashAcceptor.iTestMode = 1;
                    //Display.screen26.SetText5("3 - Switch from Return Mode to Stack Mode");
                    Display.GotoScreen(26, 0);
                    //PD- rev19b3
                    Display.screen0.SetText(Display.screen0.lData, "Status: " + CashAcceptor.iStatus + ": " + CashAcceptor.sStatus + "\nByte0: " + CashAcceptor.iStatusByte0 + " Byte1: " + CashAcceptor.iStatusByte1 + " Byte2: " + CashAcceptor.iStatusByte2);
                    //}
                }
                else if (iKey == 7 && Display.iScreenMode == 1)
                {
                    bPINGenerated = false;
                    Display.GotoScreen(27, 0);
                }
                else if (iKey == 8 && Display.iScreenMode == 1)
                {
                    //HIDisplay.GotoScreen(28, 0);
                    Process pNew = null;
                    try
                    {
                        if (File.Exists("\\SDCard\\BOOT.exe"))
                        {
                            pNew = new Process();

                            //pNew.StartInfo.WorkingDirectory = "\\HardDisk\\_INSTALL";
                            pNew.StartInfo.FileName = "\\SDCard\\BOOT.exe";

                            pNew.Start();
                        }
                        else
                        {
                            Display.ShowMessageBox("Restart Error", 3);
                        }
                    }
                    catch
                    {
                        Display.ShowMessageBox("Error", 3);
                    }
                }
                else if (iKey == 9 && Display.iScreenMode == 1)
                {
                    if (bLogMaster)//show log settings
                    {
                        Display.GotoScreen(29, 0);
                    }
                    else//other settings
                    {
                        Display.GotoScreen(29, 1);
                    }
                    //System.Diagnostics.Process.Start("shutdown", "-s -f -t 00");
                }

                //else if (ikey == 8 && display.iscreenmode == 1)
                //{
                //    if (imonitoringlevel<2) 
                //    {
                //        imonitoringlevel++;
                //    }
                //    else 
                //    {
                //        imonitoringlevel=0;
                //    }
                //    if (imonitoringlevel == 1)
                //    {
                //        display.screen0.SetText(Display.screen0.lMenu3, "7 - encryption\n\n8 - monitoring low");
                //    }
                //    else if (imonitoringlevel == 2)
                //    {
                //        display.screen0.SetText(Display.screen0.lMenu3, "7 - encryption\n\n8 - monitoring high");
                //    }
                //    else
                //    {
                //        display.screen0.SetText(Display.screen0.lMenu3, "7 - encryption\n\n8 - monitoring off");
                //    }
                //}
            }
            else if (Display.iCurrentScreen == 22)
            {
                if (iKey == 13)
                {
                    if (sCode == "99")
                    {
                        Display.GotoScreen(21, 1);
                    }
                    else if (sCode == "90")
                    {
                        Display.GotoScreen(21, 2);
                    }
                    else
                    {
                        Display.GotoScreen(21, 0);
                    }
                }
            }
            else if (Display.iCurrentScreen == 25)
            {
                if (iKey == 13)
                {
                    Display.GotoScreen(21, 1);
                }
            }
            else if (Display.iCurrentScreen == 26)
            {
                //PD- rev19b3
                //PD- rev20
                //if (iKey == 1)
                //{
                //    CashAcceptor.Return();
                //}
                //else if (iKey == 2)
                //{
                //    CashAcceptor.Stack();
                //}
                //else if (iKey == 3)
                //{
                //    if (CashAcceptor.iTestMode == 1)
                //    {
                //        CashAcceptor.iTestMode = 2;
                //        Display.screen26.SetText5("3 - Switch from Stack Mode to Return Mode");
                //    }
                //    else if (CashAcceptor.iTestMode == 2)
                //    {
                //        CashAcceptor.iTestMode = 1;
                //        Display.screen26.SetText5("3 - Switch from Return Mode to Stack Mode");
                //    }
                    
                //}
                if (iKey == 13)
                {
                    CashAcceptor.Disable();
                    bBillRequest = false;
                    Display.GotoScreen(21, 1);
                    CashAcceptor.Disable();
                }
            }
            else if (Display.iCurrentScreen == 27)
            {
                if (iKey == 15)//save
                {
                    iCurrentSetting = Convert.ToInt16(FileAccess.sSettings.Substring(FileAccess.sSettings.IndexOf("<ENC_METHOD>") + 12, 1));
                    if (iCurrentSetting != iTempEncrptionType)
                    {
                        EPP.iEncryptionMethod = iTempEncrptionType;
                        FileAccess.sSettings = FileAccess.sSettings.Replace("<ENC_METHOD>" + Convert.ToString(iCurrentSetting), "<ENC_METHOD>" + Convert.ToString(EPP.iEncryptionMethod));                     
                        if (sOS == "CE")
                        {
                            FileAccessCE.WriteFile("\\SDCard\\SPT\\settings.txt", FileAccess.sSettings);
                        }
                        else
                        {
                            File.WriteAllText("settings.txt", FileAccess.sSettings);
                        }

                        Display.GotoScreen(21, 1);
                        if (EPP.iEncryptionMethod == 0)
                        {
                            Display.ShowMessageBox("SDES encryption saved", 3);
                        }
                        else
                        {
                            Display.ShowMessageBox("TDES encryption saved", 3);
                        }
                    }
                    else
                    {
                        Display.GotoScreen(21, 1);
                    }
                }
                else if (iKey == 13)//don't save
                {
                    EPP.iEncryptionMethod = Convert.ToInt16(FileAccess.sSettings.Substring(FileAccess.sSettings.IndexOf("<ENC_METHOD>") + 12, 1));
                    Display.GotoScreen(21, 1);
                }
                else if (iKey == 2)
                {
                    if (iTempEncrptionType == 0)
                    {
                        iTempEncrptionType = 1;
                        Display.screen0.SetText(Display.screen0.lMenu2, "2 - Switch from TDES to SDES");
                    }
                    else
                    {
                        iTempEncrptionType = 0;
                        Display.screen0.SetText(Display.screen0.lMenu2, "2 - Switch from SDES to TDES");
                    }
                }
                else if (iKey == 1)
                {
                    if (bPINGenerated == false)
                    {
                        EPP.EnableEncryption(EPP.iEncryptionMethod);
                        Display.GotoScreen(9, 0);
                        bPINGenerated = true;
                    }
                }
            }
            else if (Display.iCurrentScreen == 28)
            {
                if (iKey == 13)
                {
                    Display.GotoScreen(21, 1);
                }
                else if (iKey == 1)
                {
                    System.Diagnostics.Process.Start("shutdown", "-r -f -t 00");
                }
                else if (iKey == 2)
                {
                    System.Diagnostics.Process.Start("shutdown", "-s -f -t 00");
                }
            }
            else if (Display.iCurrentScreen == 29)
            {
                if (iKey == 15)//save
                {
                    if (bLogMaster)
                    {
                        iCurrentSetting = Convert.ToInt16(FileAccess.sSettings.Substring(FileAccess.sSettings.IndexOf("<LOG_SSC>") + 9, 1));
                        if (iCurrentSetting != iTempLoggingSSC)
                        {
                            iLoggingSSC = iTempLoggingSSC;
                            FileAccess.sSettings = FileAccess.sSettings.Replace("<LOG_SSC>" + Convert.ToString(iCurrentSetting), "<LOG_SSC>" + Convert.ToString(iLoggingSSC));
                            if (sOS == "CE")
                            {
                                FileAccessCE.WriteFile("\\SDCard\\SPT\\settings.txt", FileAccess.sSettings);
                            }
                            else
                            {
                                File.WriteAllText("settings.txt", FileAccess.sSettings);
                            }
                        }
                        iCurrentSetting = Convert.ToInt16(FileAccess.sSettings.Substring(FileAccess.sSettings.IndexOf("<LOG_EPP>") + 9, 1));
                        if (iCurrentSetting != iTempLoggingEPP)
                        {
                            iLoggingEPP = iTempLoggingEPP;
                            FileAccess.sSettings = FileAccess.sSettings.Replace("<LOG_EPP>" + Convert.ToString(iCurrentSetting), "<LOG_EPP>" + Convert.ToString(iLoggingEPP));
                            if (sOS == "CE")
                            {
                                FileAccessCE.WriteFile("\\SDCard\\SPT\\settings.txt", FileAccess.sSettings);
                            }
                            else
                            {
                                File.WriteAllText("settings.txt", FileAccess.sSettings);
                            } 
                        }
                        iCurrentSetting = Convert.ToInt16(FileAccess.sSettings.Substring(FileAccess.sSettings.IndexOf("<LOG_CA>") + 8, 1));
                        if (iCurrentSetting != iTempLoggingCA)
                        {
                            iLoggingCA = iTempLoggingCA;
                            FileAccess.sSettings = FileAccess.sSettings.Replace("<LOG_CA>" + Convert.ToString(iCurrentSetting), "<LOG_CA>" + Convert.ToString(iLoggingCA));
                            if (sOS == "CE")
                            {
                                FileAccessCE.WriteFile("\\SDCard\\SPT\\settings.txt", FileAccess.sSettings);
                            }
                            else
                            {
                                File.WriteAllText("settings.txt", FileAccess.sSettings);
                            } 
                        }
                        iCurrentSetting = Convert.ToInt16(FileAccess.sSettings.Substring(FileAccess.sSettings.IndexOf("<LOG_CR>") + 8, 1));
                        if (iCurrentSetting != iTempLoggingCR)
                        {
                            iLoggingCR = iTempLoggingCR;
                            FileAccess.sSettings = FileAccess.sSettings.Replace("<LOG_CR>" + Convert.ToString(iCurrentSetting), "<LOG_CR>" + Convert.ToString(iLoggingCR));
                            if (sOS == "CE")
                            {
                                FileAccessCE.WriteFile("\\SDCard\\SPT\\settings.txt", FileAccess.sSettings);
                            }
                            else
                            {
                                File.WriteAllText("settings.txt", FileAccess.sSettings);
                            } 
                        }
                        iCurrentSetting = Convert.ToInt16(FileAccess.sSettings.Substring(FileAccess.sSettings.IndexOf("<LOG_PR>") + 8, 1));
                        if (iCurrentSetting != iTempLoggingPR)
                        {
                            iLoggingPR = iTempLoggingPR;
                            FileAccess.sSettings = FileAccess.sSettings.Replace("<LOG_PR>" + Convert.ToString(iCurrentSetting), "<LOG_PR>" + Convert.ToString(iLoggingPR));
                            if (sOS == "CE")
                            {
                                FileAccessCE.WriteFile("\\SDCard\\SPT\\settings.txt", FileAccess.sSettings);
                            }
                            else
                            {
                                File.WriteAllText("settings.txt", FileAccess.sSettings);
                            } 
                        }
                    }
                    else
                    {
                        iCurrentSetting = Convert.ToInt16(FileAccess.sSettings.Substring(FileAccess.sSettings.IndexOf("<BRAND>") + 7, 1));
                        if (iCurrentSetting != iTempBrand)
                        {
                            iBrand = iTempBrand;
                            FileAccess.sSettings = FileAccess.sSettings.Replace("<BRAND>" + Convert.ToString(iCurrentSetting), "<BRAND>" + Convert.ToString(iBrand));
                            if (sOS == "CE")
                            {
                                FileAccessCE.WriteFile("\\SDCard\\SPT\\settings.txt", FileAccess.sSettings);
                            }
                            else
                            {
                                File.WriteAllText("settings.txt", FileAccess.sSettings);
                            } 
                        }

                        iCurrentSetting = Convert.ToInt16(FileAccess.sSettings.Substring(FileAccess.sSettings.IndexOf("<PTR_TYPE>") + 10, 1));
                        if (iCurrentSetting != iTempPtrType)
                        {
                            Printer.iType = iTempPtrType;
                            FileAccess.sSettings = FileAccess.sSettings.Replace("<PTR_TYPE>" + Convert.ToString(iCurrentSetting), "<PTR_TYPE>" + Convert.ToString(Printer.iType));
                            if (sOS == "CE")
                            {
                                FileAccessCE.WriteFile("\\SDCard\\SPT\\settings.txt", FileAccess.sSettings);
                            }
                            else
                            {
                                File.WriteAllText("settings.txt", FileAccess.sSettings);
                            } 
                        }
                    }
                    Display.GotoScreen(21, 1);
                    Display.ShowMessageBox("Changes saved", 3);
                }
                else if (iKey == 13)//don't save
                {
                    //iLogging = Properties.Settings.Default.Logging;
                    Display.GotoScreen(21, 1);
                }
                else if (iKey == 1)
                {
                    if (bLogMaster)
                    {
                        if (iTempLoggingSSC == 0)
                        {
                            iTempLoggingSSC = 1;
                            Display.screen0.SetText(Display.screen0.lMenu1, "1 - SSC Logging ON");
                        }
                        else
                        {
                            iTempLoggingSSC = 0;
                            Display.screen0.SetText(Display.screen0.lMenu1, "1 - SSC Logging OFF");
                        }
                    }
                    else
                    {
                        if (iTempBrand == 2)
                        {
                            iTempBrand = 1;
                            Display.screen0.SetText(Display.screen0.lMenu1, "1 - BRAND: ARCO");
                        }
                        else
                        {
                            iTempBrand = 2;
                            Display.screen0.SetText(Display.screen0.lMenu1, "1 - BRAND: Thrifty");
                        }
                    }
                }
                else if (iKey == 2)
                {
                    if (bLogMaster)
                    {
                        if (iTempLoggingEPP == 0)
                        {
                            iTempLoggingEPP = 1;
                            Display.screen0.SetText(Display.screen0.lMenu2, "2 - EPP Logging ON");
                        }
                        else
                        {
                            iTempLoggingEPP = 0;
                            Display.screen0.SetText(Display.screen0.lMenu2, "2 - EPP Logging OFF");
                        }
                    }
                    else
                    {
                        if (iTempPtrType == 4)
                        {
                            iTempPtrType = 1;
                            Display.screen0.SetText(Display.screen0.lMenu2, "2 - PRINTER: T2 RS232");
                        } 
                        else if (iTempPtrType == 3)
                        {
                            iTempPtrType = 4;
                            Display.screen0.SetText(Display.screen0.lMenu2, "2 - PRINTER: H3 Custom");
                        }
                        else if (iTempPtrType == 2)
                        {
                            iTempPtrType = 3;
                            Display.screen0.SetText(Display.screen0.lMenu2, "2 - PRINTER: H3 USB");
                        }
                        else
                        {
                            iTempPtrType = 2;
                            Display.screen0.SetText(Display.screen0.lMenu2, "2 - PRINTER: H3 RS232");
                        }
                    }
                }
                else if (iKey == 3 && bLogMaster)
                {
                    if (iTempLoggingCA == 0)
                    {
                        iTempLoggingCA = 1;
                        Display.screen0.SetText(Display.screen0.lMenu3, "3 - CA Logging ON");
                    }
                    else
                    {
                        iTempLoggingCA = 0;
                        Display.screen0.SetText(Display.screen0.lMenu3, "3 - CA Logging OFF");
                    }
                }
                else if (iKey == 4 && bLogMaster)
                {
                    if (iTempLoggingCR == 0)
                    {
                        iTempLoggingCR = 1;
                        //CE Display.screen29.SetText4("4 - CR Logging ON");
                    }
                    else
                    {
                        iTempLoggingCR = 0;
                        //CE Display.screen29.SetText4("4 - CR Logging OFF");
                    }
                }
                else if (iKey == 5 && bLogMaster)
                {
                    if (iTempLoggingPR == 0)
                    {
                        iTempLoggingPR = 1;
                        //CE Display.screen29.SetText5("5 - PR Logging ON");
                    }
                    else
                    {
                        iTempLoggingPR = 0;
                        //CE Display.screen29.SetText5("5 - PR Logging OFF");
                    }
                }
            }
            else if (Display.iCurrentScreen == 39)
            {
                if (iKey == 13)
                {
                    if (CashAcceptor.iStatus != 3 && CashAcceptor.iStatus != 4)//don't allow while accepting
                    {
                        CashAcceptor.Disable();
                        RS485.SendCancel();
                        CashAcceptor.iBillTotal = 0;
                        CenCom.bBillRequest = false;
                    }
                }
                else if (iKey == 15)
                {
                    if (CashAcceptor.iStatus != 3 && CashAcceptor.iStatus != 4)//don't allow while accepting
                    {
                        CashAcceptor.Disable();
                        RS485.SendEnter();
                        CashAcceptor.iBillTotal = 0;
                        CenCom.bBillRequest = false;
                    }
                }
            }
            //BeepOff();
        }

        static void ProcessCancel()
        {
            CenCom.Reset();
            //Display.GotoScreen(0, 0);
            Display.MyGotoMain(1);
            RS485.SendGNI();
        }
    }

    public static class RS485
    {
        public static int iPortNum = 0;

        static SerialPort Port485 = new SerialPort();

        public static bool bWaitingForResponse = false;
        public static int iStatus = 0;

        static string sReceive = "";
        static string sTransmit = "";
        static string sPreviousReceive = "";
        static string sPreviousTransmit = "";

        public static string[] sRAM = new string[255];

        //maintain through multiple data received events
        //static int iESC = 0;
        static bool bReceive = false;
        static int iCheck = 0;
        //static int iCheckVal1, iCheckVal2;

        public static string sDataToSend;
        public static int iMsgSeq;

        public static void Init()
        {
            Port485.BaudRate = 9600;
            Port485.StopBits = StopBits.One;
            Port485.Parity = Parity.None;
            Port485.DataBits = 8;
            Port485.PortName = "COM" + iPortNum;
            Port485.Handshake = Handshake.None;
            //Port485.ReceivedBytesThreshold = 1;
            Port485.ReadTimeout = 3000;//may need to change...
            //Port485.RtsEnable = true;//pd- need for advantech rs485

            Port485.DataReceived += new SerialDataReceivedEventHandler(Port485_DataReceived);

            try
            {
                Port485.Open();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                Debug.WriteLine(ex.Message);
            }

            CenCom.bStartup = false;
        }

        static void Port485_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int iBytesToRead = Port485.BytesToRead;
            byte[] comBuffer = new byte[iBytesToRead];
            byte bCurrentByte;
            int i;

            CenCom.i485Monitor = 0;

            if (iStatus < 1)//happens only if was offline
            {
                iStatus = 1;
                Debug.WriteLine("RS485 Online");

                bWaitingForResponse = false;//pd- don't trigger block below if just switched to online (send cas now, not cancel)
            }
            else if (bWaitingForResponse == true)//happens each status check....
            {
                bWaitingForResponse = false;
                Debug.WriteLine("RS485 WaitingForResponse Off");
            }

            Debug.WriteLine("RS485 DATA RECEIVED");
            Debug.WriteLine("Bytes to Read: " + iBytesToRead);

            try
            {
                Port485.Read(comBuffer, 0, iBytesToRead);

            }
            catch
            {
                iBytesToRead = 0;
                Debug.WriteLine("RS485 READ ERROR*************");
            }

            for (i = 0; i < iBytesToRead; i++)
            {
                bCurrentByte = comBuffer[i];

                Debug.Write("PIC LINE IN: ");
                Debug.WriteLine("Byte " + i + " of " + iBytesToRead + " -> " + String.Format("{0:X2}", bCurrentByte));

                //if (bCurrentByte == 0xF0 && iESC == 0)
                //{
                //    iESC = 1;
                //} 

                //if (bCurrentByte == (0xA0 + CenCom.iPICID) && iESC == 0)
                if (bCurrentByte == (0xA0 + CenCom.iPICID) && iCheck == 0)
                {
                    Debug.WriteLine("bReceive is ON!");
                    Debug.WriteLine("PIC " + CenCom.iPICID + " STX");

                    bReceive = true;
                    iCheck = 0;
                    sReceive = "";

                    CenCom.i485Monitor = 0;

                    if (iStatus < 1)//happens only if was offline
                    {
                        iStatus = 1;
                        Debug.WriteLine("RS485 Online");

                        bWaitingForResponse = false;//pd- don't trigger block below if just switched to online (send cas now, not cancel)
                    }
                    else if (bWaitingForResponse == true)//happens each status check....
                    {
                        bWaitingForResponse = false;
                        Debug.WriteLine("RS485 WaitingForResponse Off");
                    }
                }

                if (bReceive == true)
                {
                    sReceive = sReceive + Convert.ToString((char)bCurrentByte);

                    if (iCheck > 0)
                    {
                        iCheck++;
                        if (iCheck > 2)
                        {
                            iCheck = 0;
                            bReceive = false;

                            if (CenCom.iLoggingSSC == 1)
                            {
                                LogMessage();
                            }

                            //if (ValidateMessage())
                                if (true)
                            {
                                MyProcessMessage();
                                CenCom.MyUpdateStatus();
                                MySendResponse();
                                sTransmit = "";
                            }
                            else
                            {
                                Debug.WriteLine("CRC FAILURE!!!");
                                MySendError(1, "");
                                MySendResponse();
                            }                       
                        }
                    }
                    else if (bCurrentByte == 0xF3)
                    {
                        iCheck = 1;
                    }
                }
                else
                {
                    //IGNORE BYTE
                }
            }
        }

        static bool ValidateMessage()
        {
            string sMyCRC, sSentCRC;

            Debug.WriteLine("VALIDATE MESSAGE");

            sSentCRC = sReceive.Substring(sReceive.Length - 2, 2);
            sMyCRC = Crc.ComputeChecksum(sReceive.Substring(0, sReceive.Length - 2));

            Debug.Write("My CRC = ");
            foreach (char c in sMyCRC)
            {
                Debug.Write(String.Format("{0:X2}", (byte)c) + " ");
            }

            if (sSentCRC == sMyCRC)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void MySendBill(int iValue)
        {
            sTransmit = sTransmit + Convert.ToString((char)0x09) + Convert.ToString((char)iValue);
        }

        public static void MySendKey(int iKey)
        {
            sTransmit = sTransmit + Convert.ToString((char)0x02) + Convert.ToString((char)iKey);
        }

        public static void MySendStatus(int iComponent, int iStatus)
        {
            sTransmit = sTransmit + Convert.ToString((char)(0x10 + iComponent)) + Convert.ToString((char)iStatus);
        }

        public static void MySendError(int iCode, string sError)
        {
            sTransmit = Convert.ToString((char)0x0E) + Convert.ToString((char)iCode) + Convert.ToString((char)sError.Length) + sError;
        }

        public static void MyStartTransaction(int iPump, int iType)
        {
            Debug.WriteLine("Start Transaction - Pump = " + iPump + "Type = " + iType);
            sTransmit = Convert.ToString((char)0x01) + Convert.ToString((char)iPump) + Convert.ToString((char)iType);
        }

        static void MySendResponse()
        {
            string sMsg = "";
            int i;
            byte[] bbTransmit;

            Debug.Write("SEND RESPONSE: ");

            sMsg = Convert.ToString((char)(0xB0 + CenCom.iPICID)) + sTransmit + Convert.ToString((char)0xF3);
            sMsg = sMsg + Crc.ComputeChecksum(sMsg);
            sTransmit = sMsg;

            bbTransmit = new byte[sTransmit.Length];

            i = 0;
            foreach (char c in sTransmit)
            {
                Debug.Write(String.Format("{0:X2}", (byte)c) + " ");
                bbTransmit[i] = (byte)c;
                i++;
            }
            Debug.WriteLine(">>>");

            Port485.Write(bbTransmit, 0, bbTransmit.Length);
        }

        static void MyProcessMessage()
        {
            int i;
            int iCmd = 0;

            Debug.Write("PROCESS MSG: ");
            foreach (char c in sReceive)
            {
                Debug.Write(String.Format("{0:X2}", (byte)c) + " ");
            }
            Debug.WriteLine("<<<");

            i = 1;
            while (i < (sReceive.Length - 3))
            {
                iCmd = sReceive[i];

                try
                {
                    if (iCmd == 0x04) //Cash Acceptor
                    {
                        CenCom.displayBill = true;
                        if (sReceive[i + 1] == 0)
                        {
                            CashAcceptor.Disable();
                        }
                        else if (sReceive[i + 1] == 1)
                        {
                            CashAcceptor.Enable();
                        }
                        else if (sReceive[i + 1] == 2)
                        {
                            CashAcceptor.Stack();
                        }
                        else if (sReceive[i + 1] == 3)
                        {
                            CashAcceptor.Return();
                        }
                        i = i + 2;
                    }
                    else if (iCmd == 0x03) //Status Request
                    {
                        //
                        Debug.WriteLine("Reached");
                        //Send back all the statuses
                        //Send EPP Status
                        MySendStatus(1, EPP.iStatus);
                        //Send CA Status
                        MySendStatus(4, CashAcceptor.iStatus);
                        MySendStatus(5, Printer.iStatus);
                        //MySendStatus(3, 0);
                        i = i + 2;


                    }
                    else if (iCmd == 0x10)
                    {
                        //Parse the string until F0

                        Int16 mylength = Convert.ToInt16(sReceive[i + 2]);
                        
                        CenCom.MySetString(Convert.ToInt16(sReceive[i + 1]), sReceive.Substring(i + 3, Convert.ToInt16(sReceive[i + 2])));
                        i = i + 3 + Convert.ToInt16(sReceive[i + 2]);
                    }
                    else if (iCmd == 0x11)
                    {
                        Display.MyGotoMain(sReceive[i + 1]);
                        i = i + 3;
                    }
                    else if (iCmd == 0x12)
                    {
                        //Display.MyShowMsg(sReceive[i + 1]);
                        i = i + 3;
                    }
                    else if (iCmd == 0x13)
                    {
                                               
                        Display.MyRequestCashCard(sReceive[i + 1]);
                        i = i + 3;
                    }
                    else if (iCmd == 0x14)
                    {
                        if (CenCom.displayBill)
                        {
                            Display.MyShowCashTotal(sReceive[i + 2]);
                            CenCom.displayBill = false;
                        }
                        i = i + 3;
                    }
                    else if (iCmd == 0x16)
                    {
                        int iPrintLength = 0;
                        //For each char in sReceive until F0,F3 add to string
                        int pIndex = i+1;
                        string sPrintString = "";
                        bool firstBreak = false;
                        bool secondBreak = false;
                        bool continueRead = true;
                        while (continueRead)
                        {
                            
                            
                            if(sReceive[pIndex] == 0xF0 && sReceive[pIndex+1] == 0xF3)
                            {
                                continueRead = false;

                            }
                            else
                            {
                                iPrintLength++;
                                pIndex++;
                            }
                            
                        }
                        //Print the string with substring
                        Printer.Print(sReceive.Substring(i + 1, iPrintLength));
                        i = i + 3 + iPrintLength;
                    }
                    else if (iCmd == 0x20)
                    {
                        //If the index is 86 set string to "Accepting Bill...Please Wait"
                        int index = Convert.ToInt16(sReceive[i + 1]);
                        if(index == 86)
                        {
                            //CenCom.MySetString(86, "Accepting Bill...Please Wait");
                            //Display.ShowMessageBox(CenCom.MyGetString(index), 1);
                           
                        }
                        else if(index == 99)
                        {
                            CenCom.MySetString(99, "Your Pump is now Ready\n\nPlease return for your Receipt\n\nChange Available Inside at Cashier");
                            Display.ShowMessageBox(CenCom.MyGetString(index), 5);
                        }
                        else if(index == 100)
                        {
                            CenCom.MySetString(100, "Sorry your Pump was not Reserved\nPlease see Cashier for Change");
                            Display.ShowMessageBox(CenCom.MyGetString(index), 5);
                        }
                        //Display.MyShowMsg(sReceive[i + 1]);
                        Display.ShowMessageBox(CenCom.MyGetString(index), 5);
                        i = i + 2;
                    }
                    else
                    {
                        MySendError(2, "Cmd " + String.Format("{0:X2}", iCmd) + " Not Found");
                        break;
                    }
                }
                catch
                {
                    MySendError(3, "Cmd " + String.Format("{0:X2}", iCmd) + " Failed");
                    break;
                }
            }
        }

        static void LogMessage()
        {
            try
            {
                if (sPreviousReceive != sReceive)
                {
                    WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                    foreach (char c in sReceive)
                    {
                        WriteLogData_RX(String.Format("{0:X2}", (byte)c) + " ");
                    }
                }
                else
                {
                    WriteLogData_RX("R");
                }
            }
            catch
            {

            }
            sPreviousReceive = sReceive;
        }

        static void LogResponse()
        {
            try
            {
                if (sPreviousTransmit != sTransmit)
                {
                    WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                    foreach (char c in sTransmit)
                    {
                        WriteLogData_RX(String.Format("{0:X2}", (byte)c) + " ");
                    }
                }
                else
                {
                    WriteLogData_RX("R");
                }
            }
            catch
            {

            }
            sPreviousTransmit = sTransmit;
        }

        static string FormatNum(int iNum, int iDigits)
        {
            int iPad = 0;
            string sPad = "";

            if (iNum < 10)
            {
                iPad = iDigits - 1;
            }
            else if (iNum < 100)
            {
                iPad = iDigits - 2;
            }
            else if (iNum < 1000)
            {
                iPad = iDigits - 3;
            }

            while (iPad > 0)
            {
                sPad = sPad + "0";
                iPad--;
            }
            return (sPad + Convert.ToString(iNum));
        }

        public static void SendConfig()
        {
            string sSend;
            sSend = GetCAS() + GetCRS() + GetPRS() + GetGNS() + "DSS001AKBS001A";//PD - rev17b3
            sSend = GetGNS() + "GNR000" + GetCRS() + GetCAS() + "DSS001AKBS001A" + GetPRS();//PD - rev19
            //Debug.WriteLine(sSend);
            SendData(sSend);
        }

        public static string GetGNS()
        {
            string sSend;

            sSend = "GNS003";

            if (CenCom.bMainDoorOpen)
            {
                sSend = sSend + "O";
                //sSend = sSend + "C";
            }
            else
            {
                sSend = sSend + "C";
            }

            sSend = sSend + "C";

            if (CenCom.bAlarmOn)
            {
                sSend = sSend + "Y";
            }
            else
            {
                sSend = sSend + "N";
            }

            return sSend;
        }

        public static void SendGNS()
        {
            string sSend;

            sSend = GetGNS();
            SendData(sSend);
        }

        public static string GetCAS()
        {
            string sSend;

            sSend = "CAS008";

            if (CashAcceptor.iStatus == -2)
            {
                return sSend + "0004900N";//failure
            }
            else if (CashAcceptor.iStatus == -1)
            {
                //if (CashAcceptor.bCassetteRemoved == true)
                //{
                //    CashAcceptor.bCassetteRemoved = false;
                //}
                return sSend + "0001B00N";//power up
            }
            else if (CashAcceptor.iStatus == 1)
            {
                //if (CashAcceptor.bCassetteRemoved == true)
                //{
                //    CashAcceptor.bCassetteRemoved = false;
                //}
                return sSend + "0001A00N";//disabled
            }
            else if (CashAcceptor.iStatus == 2)
            {
                //if (CashAcceptor.bCassetteRemoved == true)//shouldn't neet to do this after forcing this state to not exist unless bBillRequest=true.
                //{
                //    CashAcceptor.bCassetteRemoved = false;
                //    return sSend + "0001B00N";//initializing... to prevent error msgs
                //}
                //else
                //{
                    return sSend + "0001100N";//enabled
                //}
            }
            else if (CashAcceptor.iStatus == 3)
            {
                return sSend + "0001200N";//accepting
            }
            else if (CashAcceptor.iStatus == 4)
            {
                return sSend + FormatNum(CashAcceptor.iBillVal,3) + "1300N";//escrowed
            }
            else if (CashAcceptor.iStatus == 5)
            {
                //PD- rev 16 - iStatus won't equal 5 if billval = 0
                //if (CashAcceptor.iBillVal > 0)//set to 0 when cassette removed
                //{
                    return sSend + FormatNum(CashAcceptor.iBillVal, 3) + "1400N";//stacking
                //}
                //else
                //{
                //    return "";
                //}
            }
            //else if (CashAcceptor.iStatus == 6)
            //{
                //if (CashAcceptor.iBillVal > 0)//set to 0 when cassette removed
                //{
                    
                //}
                //else
                //{
                //    return "";
                //}
            //}
            else if (CashAcceptor.iStatus == 7)
            {
                return sSend + FormatNum(CashAcceptor.iBillVal, 3) + "1500N" + sSend + FormatNum(CashAcceptor.iBillVal, 3) + "1600N";//vend valid + stacked
            }
            else if (CashAcceptor.iStatus == 8)
            {
                return sSend + FormatNum(CashAcceptor.iBillVal, 3) + "1800Y";//returning
            }
            else if (CashAcceptor.iStatus == 11)
            {
                return sSend + "0004500N";//bill jam
            }
            else if (CashAcceptor.iStatus == 12)
            {
                return sSend + "0004300N";//cassette full
            }
            else if (CashAcceptor.iStatus == 13)
            {
                return sSend + "0004400N";//cassette removed
            }
            else if (CashAcceptor.iStatus == 14)
            {
                return sSend + "0004800N";//cheat or reject
            }
            else if (CashAcceptor.iStatus == 15)
            {
                return sSend + "0004700Y";//paused. something inserted after bill is in process of stacking
            }
            else//CashAcceptor.iStatus  == 0
            {
                return sSend + "000F100N";
            }
        }

        public static void SendCAS()
        {
            string sSend;
            sSend = CashAcceptor.iStatus.ToString();
            Debug.WriteLine(sSend);
            RS485.MySendStatus(4, CashAcceptor.iStatus);
        }

        public static string GetCRS()
        {
            string sSend;

            sSend = "CRS001";

            if (CardReader.iStatus == 1)
            {
                return sSend + "D";
            }
            else if (CardReader.iStatus == 2)
            {
                return sSend + "C";
            }
            else if (CardReader.iStatus == 3)
            {
                return sSend + "E";
            }
            else//CardReader.iStatus  == 0
            {
                return sSend + "O";
            }
        }

        public static void SendCRS()
        {
            string sSend;

            sSend = GetCRS();
            SendData(sSend);
        }

        public static string GetPRS()
        {
            string sSend;

            sSend = "PRS003";

            if (Printer.iStatus == 1)
            {
                return sSend + "ANN";
            }
            else if (Printer.iStatus == 2)
            {
                return sSend + "PNN";
            }
            else//Printer.iStatus  == 0
            {
                return sSend + "ONN";
            }
        }

        public static void SendPRS()
        {
            string sSend;

            sSend = GetPRS();
            SendData(sSend);
        }

        public static void SendGNI()
        {
            SendData("GNI000");
        }

        public static void SendEnter()
        {
            sTransmit = sTransmit + Convert.ToString((char)0x05) + Convert.ToString((char)15);
            //SendData("0X");
        }
        public static void SendKeyPress(int iPassKey)
        {
            sTransmit = sTransmit + Convert.ToString((char)0x05) + Convert.ToString((char)iPassKey);
        }
        public static void SendCancel()
        {
            sTransmit = sTransmit + Convert.ToString((char)0x05) + Convert.ToString((char)13);
        }

        public static void SendClear()
        {
            SendData("KBF001A");
        }

        public static void SendEspanol()
        {
            SendData("KBF001R");
        }

        public static void SendF1()
        {
            SendData("KBF001a");
        }

        public static void SendF2()
        {
            SendData("KBF001b");
        }

        public static void SendF3()
        {
            SendData("KBF001c");
        }

        public static void SendF4()
        {
            SendData("KBF001d");
        }

        public static void PumpGas(string sData)
        {
            string sSend = "";
            
            sSend = "KBF001a" + "KBN00" + (sData.Length + 1) + "B" + sData;//length should never exceed 9 digits

            Debug.WriteLine("Auth Pump: " + sSend);
            SendData(sSend);
        }

        public static void GetReceipt(string sData)
        {
            string sSend = "";

            sSend = "KBF001b" + "KBN00" + (sData.Length + 1) + "B" + sData;//length should never exceed 9 digits

            Debug.WriteLine("Get Receipt: " + sSend);
            SendData(sSend);
        }

        public static void MyGetReceipt(int iPump)
        {
            Debug.WriteLine("Get Receipt Start- Pump = " + iPump);

            sTransmit = Convert.ToString((char)0x16) + Convert.ToString((char)iPump);
            //Printer.Print(CenCom.MyGetString(161));

        }

        public static void GetReceiptPart1()
        {
            string sSend = "";

            sSend = "KBF001b";
            CenCom.bPrintWait = true;
            RS485.MyGetReceipt(Convert.ToInt16(CenCom.sPump));


            Debug.WriteLine("Get Receipt - Part 1: " + sSend);
            SendData(sSend);
        }

        public static void GetReceiptPart2(string sData)
        {
            string sSend = "";

            sSend = "KBN00" + (sData.Length + 1) + "B" + sData;//length should never exceed 9 digits

            Debug.WriteLine("Get Receipt - Part 2: " + sSend);
            SendData(sSend);
        }

        public static void PreSwipe()
        {
            string sSend = "";

            sSend = "CRS001P" + "KBF001a";

            Debug.WriteLine("Pre Swipe: " + sSend);
            SendData(sSend);
        }

        public static void SendInput(string sData)
        {
            string sSend = "";

            //Debug.WriteLine("Trying to reserve pump " + CenCom.sPump);
            //SendData("DPTI1Nn01KBF001aKBN002B" + CenCom.sPump);
            sSend = "KBN00" + (sData.Length + 1) + "B" + sData;//length should never exceed 9 digits
            
            Debug.WriteLine("Sending Input: " + sSend);
            SendData(sSend);
        }

        public static void SendCardData()
        {
            string sLengthCR1;
            string sLengthCR2;

            //CardReader.sTrack1 = "";
            //CardReader.sTrack2 = ";65000950000000005=0012?0";

            sLengthCR1 = FormatNum(CardReader.sTrack1.Length, 3);
            sLengthCR2 = FormatNum(CardReader.sTrack2.Length, 3);

            Debug.WriteLine("SEND CARD DATA");
            Debug.WriteLine("CR1= " + CardReader.sTrack1);
            Debug.WriteLine("CR2= " + CardReader.sTrack2);
            Debug.WriteLine("PAN= "+CardReader.sPAN);

            SendData("CR1" + sLengthCR1 + CardReader.sTrack1 + "CR2" + sLengthCR2 + CardReader.sTrack2 + "CRS001E");
        }

        public static string GetCardData()
        {
            string sLengthCR1;
            string sLengthCR2;

            //CardReader.sTrack1 = "";
            //CardReader.sTrack2 = ";65000950000000005=0012?0";

            sLengthCR1 = FormatNum(CardReader.sTrack1.Length, 3);
            sLengthCR2 = FormatNum(CardReader.sTrack2.Length, 3);

            Debug.WriteLine("SEND CARD DATA");
            Debug.WriteLine("CR1= " + CardReader.sTrack1);
            Debug.WriteLine("CR2= " + CardReader.sTrack2);
            Debug.WriteLine("PAN= " + CardReader.sPAN);

            return("CR1" + sLengthCR1 + CardReader.sTrack1 + "CR2" + sLengthCR2 + CardReader.sTrack2 + "CRS001E");
        }

        public static void SendPINData()
        {
            string sLengthKBP;

            sLengthKBP = FormatNum(EPP.EPB.Length + EPP.KSN.Length + 1, 3);
            
            Debug.WriteLine("SEND PIN DATA");
            Debug.WriteLine("EPB= " + EPP.EPB);
            Debug.WriteLine("KSN= " + EPP.KSN);

            SendData("KBP" + sLengthKBP + "B" + EPP.EPB + EPP.KSN);
        }

        public static void QuestionResponse1(bool bAnswer)
        {
            if (bAnswer)
            {
                SendF3();
            }
            else
            {
                SendF4();
            }
        }

        public static void RequestArmoredReport()
        {
            SendData("GNI000GNA000");
        }

        static void SendData(string sString)
        {
            if (iStatus == 1)
            {
                sDataToSend = sDataToSend + sString;
            }
            else
            {
                Debug.WriteLine("RS485 offline, string not sent: " + sString);
            }

        }

        static void WriteLogData_RX(string sData)
        {
            try
            {
                if (CenCom.sOS == "CE")
                {
                    FileAccessCE.AppendFile("\\SDCard\\SPT\\LOGS\\RS_" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
                else
                {
                    File.AppendAllText("LOGS\\RS_" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
            }
            catch
            {

            }
        }

        public static void WriteLogData_TX(string sData)
        {
            try
            {
                if (CenCom.sOS == "CE")
                {
                    FileAccessCE.AppendFile("\\SDCard\\SPT\\LOGS\\RS_" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
                else
                {
                    File.AppendAllText("LOGS\\RS_" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
            }
            catch
            {

            }
        }
    }

    public static class Display
    {
        public static int iCurrentScreen;
        public static int iScreenMode;
        public static int iMaxInput;
        public static int iMinInput;

        public static bool bMsgBoxShowing = false;

        //public static Form fCurrentForm;

        public static Form0 screen0 = new Form0();
        //public static Form4 screen4 = new Form4();

        public static int iType = 1;

        public static void Init()
        {
            iCurrentScreen = 0;
            iScreenMode = 0;
            iMaxInput = 2;

            if(CenCom.inputType == 1)
            {
                //Disable Touch Screen Buttons
                screen0.HideButton(screen0.key1);
                screen0.HideButton(screen0.button1);
                screen0.HideButton(screen0.button2);
                screen0.HideButton(screen0.button3);
                screen0.HideButton(screen0.button4);
                screen0.HideButton(screen0.button5);
                screen0.HideButton(screen0.button6);
                screen0.HideButton(screen0.button7);
                screen0.HideButton(screen0.button8);
                screen0.HideButton(screen0.button9);
                screen0.HideButton(screen0.button10);
                screen0.HideButton(screen0.button11);
                screen0.HideButton(screen0.button12);
            }
            else
            {
                screen0.ChangeCursor(true);
            }
            

            //if (iType == 2)
            //{
            //    myParent.Width = 804;
            //    myParent.Height = 604;
            //}
            //else
            //{
            //    myParent.Width = 804;
            //    myParent.Height = 484;
            //}

            screen0.ShowThis();
            //screen4.ShowThis();

            //myParent.Show();

        }

        public static void MyGotoMain(int iIndex)
        {
            Debug.WriteLine("GOTOMAIN: iIndex = " + iIndex + " = " + CenCom.MyGetString(iIndex));

            //screen0.SetText(Display.screen0.lPromptTop, CenCom.MyGetString(iIndex));
            GotoScreen(1, 0);
        }

        public static void MyShowMsg(int iIndex)
        {
            Debug.WriteLine("SHOW MESSAGE: iIndex = " + iIndex + " = " + CenCom.MyGetString(iIndex));

            screen0.SetText(Display.screen0.lMsg, CenCom.MyGetString(iIndex));
            GotoScreen(20, 0);
        }

        public static void MyShowCashTotal(int iTotal)
        {
            Debug.WriteLine("SHOW CASH TOTAL: iTotal = " + iTotal);

            //screen0.SetText(Display.screen0.lMsg, CenCom.MyGetString(iIndex));
            Display.screen0.SetText(Display.screen0.lPromptTop, "Press ENTER after last bill\nTotal Amount Accepted:");
            Display.screen0.SetText(Display.screen0.lPromptBottom, "");
            Display.screen0.SetText(Display.screen0.lInput, "$" + Convert.ToString(iTotal)); 
            GotoScreen(7, 0);
        }

        public static void MyRequestPumpNum(int iIndex)
        {
            screen0.SetText(Display.screen0.lMsg, CenCom.MyGetString(iIndex));
            GotoScreen(2, 0);
        }

        public static void MyRequestInput(int iIndex)
        {
            screen0.SetText(Display.screen0.lMsg, CenCom.MyGetString(iIndex));
            GotoScreen(0, 0);
        }

        public static void MyRequestChoice(int iIndex)
        {
            screen0.SetText(Display.screen0.lMsg, CenCom.MyGetString(iIndex));
            GotoScreen(8, 0);
        }

        public static void MyRequestCashCard(int iIndex)
        {
            screen0.SetText(Display.screen0.lMsg, CenCom.MyGetString(iIndex));
            GotoScreen(3, 0);
        }
        public static void ChangeScreen(int iNextScreen, int iMode)
        {

        }

        public static void ShowKeypad()
        {
            screen0.ShowButton(screen0.key1);
            screen0.ShowButton(screen0.button1);
            screen0.ShowButton(screen0.button2);
            screen0.ShowButton(screen0.button3);
            screen0.ShowButton(screen0.button4);
            screen0.ShowButton(screen0.button5);
            screen0.ShowButton(screen0.button6);
            screen0.ShowButton(screen0.button7);
            screen0.ShowButton(screen0.button8);
            screen0.ShowButton(screen0.button9);
            screen0.ShowButton(screen0.button10);
            screen0.ShowButton(screen0.button11);
            screen0.ShowButton(screen0.button12);
        }

        public static void HideKeypad()
        {
            screen0.HideButton(screen0.key1);
            screen0.HideButton(screen0.button1);
            screen0.HideButton(screen0.button2);
            screen0.HideButton(screen0.button3);
            screen0.HideButton(screen0.button4);
            screen0.HideButton(screen0.button5);
            screen0.HideButton(screen0.button6);
            screen0.HideButton(screen0.button7);
            screen0.HideButton(screen0.button8);
            screen0.HideButton(screen0.button9);
            screen0.HideButton(screen0.button10);
            screen0.HideButton(screen0.button11);
            screen0.HideButton(screen0.button12);

        }

        public static void GotoScreen(int iNextScreen, int iMode)
        {
            //Form fOld = Form.ActiveForm.ActiveMdiChild;
            //Form.ActiveForm.ActiveMdiChild.HideThis();
            //Form.ActiveForm.HideThis();
            if (iNextScreen == 9 && iCurrentScreen == 9)
            {
                //nothing
            }
            else
            {
                CenCom.sPINEntry = "";
            }

            iCurrentScreen = iNextScreen;
            iScreenMode = iMode;
            iMinInput = 1; // just change if needed

            if (CenCom.bTestMode == false)
            {
                if (iCurrentScreen == 14 && iScreenMode == 20)
                {
                    CenCom.iScreenTimer = 10;//please take your receipt....
                }
                else
                {
                    CenCom.iScreenTimer = 0;//put here for safety and to handle nav from screen 3,4,5.
                }
            }
            else
            {
                CenCom.iScreenTimer = 60;//also set on any key press if btestmode on processkey handler
            }


            if (iNextScreen != 20 && iMode != 1)//pd- turn off card request unless screen = please remove card
            {
                CenCom.bCardRequest = false;
                Debug.WriteLine("******************bCardRequest3= " + CenCom.bCardRequest);
            }
            Debug.WriteLine("GOTOSCREEN " + iNextScreen + ", " + iMode);

            if (iNextScreen == 0)
            {
                CenCom.sInput = "";
                screen0.SetText(Display.screen0.lInput, "");

                if (iMode == 1)
                {
                    CenCom.iScreenTimer = 60;

                    iMinInput = 4;
                    iMaxInput = 4; 
                    
                    if (EPP.iType == 2)
                    {
                        screen0.SetText(Display.screen0.lPrompt, "Enter Passcode\n\n\n\n\nPress OK when done");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lPrompt, "Enter Passcode\n\n\n\n\nPress ENTER when done");
                    }
                }
                else if (iMode == 2)
                {
                    iMinInput = 5;
                    iMaxInput = 5;

                    if (CenCom.bSpanish)
                    {
                        if (EPP.iType == 2)
                        {
                            screen0.SetText(Display.screen0.lPrompt, "Marcar el c" + Convert.ToChar(243) + "digo postal.\n\n\n\n\nPresionar OK al terminar.");
                        }
                        else
                        {
                            screen0.SetText(Display.screen0.lPrompt, "Marcar el c" + Convert.ToChar(243) + "digo postal.\n\n\n\n\nPresionar ENTER al terminar.");
                        }
                    }
                    else
                    {
                        if (EPP.iType == 2)
                        {
                            screen0.SetText(Display.screen0.lPrompt, "Enter ZIP Code\n\n\n\n\nPress OK when done");
                        }
                        else
                        {
                            screen0.SetText(Display.screen0.lPrompt, "Enter ZIP Code\n\n\n\n\nPress ENTER when done");
                        }
                    }
                }
                else if (iMode == 3)
                {
                    //min input = default (1)
                    iMaxInput = 7;
                    if (CenCom.bSpanish)
                    {
                        if (EPP.iType == 2)
                        {
                            screen0.SetText(Display.screen0.lPrompt, "Marcar la lectura del od" + Convert.ToChar(243) + "metro.\n\n\n\n\nPresionar OK al terminar.");
                        }
                        else
                        {
                            screen0.SetText(Display.screen0.lPrompt, "Marcar la lectura del od" + Convert.ToChar(243) + "metro.\n\n\n\n\nPresionar ENTER al terminar.");
                        }
                    }
                    else
                    {
                        if (EPP.iType == 2)
                        {
                            screen0.SetText(Display.screen0.lPrompt, "Enter Odometer\n\n\n\n\nPress OK when done");
                        }
                        else
                        {
                            screen0.SetText(Display.screen0.lPrompt, "Enter Odometer\n\n\n\n\nPress ENTER when done");
                        }
                    }
                }
                else if (iMode == 5)
                {
                    //min input = default (1)
                    iMaxInput = 6;
                    if (CenCom.bSpanish)
                    {
                        if (EPP.iType == 2)
                        {
                            screen0.SetText(Display.screen0.lPrompt, "Marcar el n" + Convert.ToChar(250) + "mero del conductor.\n\n\n\n\nPresionar OK al terminar.");
                        }
                        else
                        {
                            screen0.SetText(Display.screen0.lPrompt, "Marcar el n" + Convert.ToChar(250) + "mero del conductor.\n\n\n\n\nPresionar ENTER al terminar.");
                        }
                    }
                    else
                    {
                        if (EPP.iType == 2)
                        {
                            screen0.SetText(Display.screen0.lPrompt, "Enter Driver #\n\n\n\n\nPress OK when done");
                        }
                        else
                        {
                            screen0.SetText(Display.screen0.lPrompt, "Enter Driver #\n\n\n\n\nPress ENTER when done");
                        }
                    }
                }
                else if (iMode == 6)
                {
                    //min input = default (1)
                    iMaxInput = 11;

                    if (CenCom.bSpanish)
                    {
                        if (EPP.iType == 2)
                        {
                            screen0.SetText(Display.screen0.lPrompt, "Marcar el n" + Convert.ToChar(250) + "mero del veh" + Convert.ToChar(237) + "culo.\n\n\n\n\nPresionar OK al terminar.");
                        }
                        else
                        {
                            screen0.SetText(Display.screen0.lPrompt, "Marcar el n" + Convert.ToChar(250) + "mero del veh" + Convert.ToChar(237) + "culo.\n\n\n\n\nPresionar ENTER al terminar.");
                        }
                    }
                    else
                    {
                        if (EPP.iType == 2)
                        {
                            screen0.SetText(Display.screen0.lPrompt, "Enter Vehicle #\n\n\n\n\nPress OK when done");
                        }
                        else
                        {
                            screen0.SetText(Display.screen0.lPrompt, "Enter Vehicle #\n\n\n\n\nPress ENTER when done");
                        }
                    }
                }
                else if (iMode == 7)
                {
                    //min input = default (1)
                    iMaxInput = 6;

                    if (CenCom.bSpanish)
                    {
                        screen0.SetText(Display.screen0.lPrompt, "Marcar ID #\n\n\n\n\nPresionar ENTER al terminar.");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lPrompt, "Enter ID #\n\n\n\n\nPress ENTER when done");
                    }
                }
                else if (iMode == 4)
                {
                    CenCom.sInput = CenCom.sPICID;
                    iMaxInput = 1;
                    if (EPP.iType == 2)
                    {
                        screen0.SetText(Display.screen0.lPrompt, "Enter PIC ID\n\n\n\n\nPress OK when done");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lPrompt, "Enter PIC ID\n\n\n\n\nPress ENTER when done");
                    }
                    screen0.SetText(Display.screen0.lInput, CenCom.sPICID);
                }
                else
                {
                    //PD- no timer should be here.  timer set after key press...

                    CashAcceptor.bHold = false;//PD - rev22

                    iMaxInput = 2;
                    ShowKeypad();

                    if (CenCom.iPICID > 0)
                    {
                        screen0.SetText(Display.screen0.lPrompt, "\nOut of Service");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lPrompt, "Out of Service\n\n\n\n\nPIC ID not set.");
                    }
                }
                screen0.ShowThis();
                ShowScreen(0, 0);
            }
            else if (iNextScreen == 1)
            {
                if (CenCom.iBrand == 2)
                {
                    screen0.HideImage(screen0.pbLogo1);
                    screen0.ShowImage(screen0.pbLogo2);
                }
                else
                {
                    screen0.HideImage(screen0.pbLogo2);
                    screen0.ShowImage(screen0.pbLogo1);
                }

                CashAcceptor.bHold = false;//PD - rev22

                //CenCom.sPINEntry = "";//PD- rev 15

                //EPP.Initialize();//in case time out after show pin entry screen...checkstatus will do this too, but this is immediate
                CenCom.ImmediateCheck();//check status of printer to know if can print receipt + report updated status of other components

                CashAcceptor.iBillTotal = 0;
                //CashAcceptor.Disable();//PD - OLD MEI - NOT IMMEDIATE IN CASE CAR OR CAA SENT WITH SCREEN 1 - pd- shouldn't need this here but just in case to disable ca faster
                CenCom.bBillRequest = false;

                if (CashAcceptor.iStatus == 15)//PD- remind SSC of paused state
                {
                    RS485.SendCAS();
                }

                CardReader.Reset();
                CenCom.bCardRequest = true;

                CenCom.bPreSwipe = false;
                CenCom.bPreInput = false;

                CenCom.bCashTransaction = false;
                screen0.SetText(Display.screen0.lPromptTop, "Please enter your Pump Number\nand press Enter");
                ShowScreen(1, 0);

                //if (CenCom.bStartup)
                //{
                //    myMessageBox.BringToFront();
                //}
            }
            else if (iNextScreen == 2)
            {
                CenCom.iScreenTimer = 10;

                iMaxInput = 2;
                if (CenCom.bPreInput == false)
                {
                    CenCom.sInput = "";
                    screen0.SetText(Display.screen0.lInput2, "");
                }
                CenCom.bPreInput = false;
                ShowScreen(2, 0);
            }
            else if (iNextScreen == 3)
            {
                //CenCom.iScreenTimer = 20;PD-so don't timeout when bill is left hanging... why need this anyway??

                if (CenCom.bSpanish)
                {
                    screen0.SetText(Display.screen0.lPromptBottom, "\n\n\nBomba " + CenCom.sPump + " seleccionada.");
                }
                else
                {
                    screen0.SetText(Display.screen0.lPromptBottom, "\n\n\nPump " + CenCom.sPump + " selected.");
                }

                if (iMode == 0)
                {
                    CenCom.bBillRequest = true;//placed here to allow more bills after bill returned
                    if (CenCom.bSpanish)
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insertar efectivo");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insert cash");
                    }
                    screen0.SetCashAnimLocation(250);
                    //screen0.SetCashAnimLocation(247);
                    //screen0.ShowImage(screen0.pbCashAnim);
                    //screen0.HideImage(screen0.pbCardAnim);
                    ShowScreen(3, 2);
                }
                else if (iMode == 1)
                {
                    CardReader.Reset();
                    CenCom.bCardRequest = true;

                    if (CenCom.bSpanish)
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insertar tarjeta");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insert card");
                    }
                    screen0.SetCardAnimLocation(336);
                    //screen0.HideImage(screen0.pbCashAnim);
                    //screen0.ShowImage(screen0.pbCardAnim);
                    ShowScreen(3, 1);
                }
                else if (iMode == 11)
                {
                    CardReader.Reset();
                    CenCom.bCardRequest = true;
                    if (CenCom.bSpanish)
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insertar tarjeta de d" + Convert.ToChar(233) + "bito");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insert debit card");
                    }
                    screen0.SetCardAnimLocation(336);
                    //screen0.HideImage(screen0.pbCashAnim);
                    //screen0.ShowImage(screen0.pbCardAnim);
                    ShowScreen(3, 11);
                }
                else if (iMode == 2)
                {
                    CardReader.Reset();
                    CenCom.bCardRequest = true;
                    CenCom.bBillRequest = true;//placed here to allow more bills after bill returned
                    if (CenCom.bSpanish)
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insertar efectivo o tarjeta");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insert cash or card");
                    }
                    screen0.SetCashAnimLocation(250);
                    //screen0.SetCashAnimLocation(115);
                    //screen0.SetCardAnimLocation(444);
                    //screen0.ShowImage(screen0.pbCashAnim);
                    //screen0.ShowImage(screen0.pbCardAnim);
                    ShowScreen(3, 2);
                }
                else if (iMode == 22)
                {
                    CardReader.Reset();
                    CenCom.bCardRequest = true;
                    CenCom.bBillRequest = true;//placed here to allow more bills after bill returned
                    if (CenCom.bSpanish)
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insertar efectivo o tarjeta de d" + Convert.ToChar(233) + "bito");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insert cash or debit card");
                    }
                    screen0.SetCashAnimLocation(115);
                    screen0.SetCardAnimLocation(444);
                    //screen0.ShowImage(screen0.pbCashAnim);
                    //screen0.ShowImage(screen0.pbCardAnim);
                    ShowScreen(3, 22);
                }
                else if (iMode == 4)
                {
                    CardReader.Reset();
                    CenCom.bCardRequest = true;
                    if (CenCom.bSpanish)
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insertar tarjeta o presionar CANCEL para usar efectivo en otra terminal");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insert card or press CANCEL to use cash at another terminal");
                    }
                    screen0.SetCardAnimLocation(336);
                    //screen0.HideImage(screen0.pbCashAnim);
                    //screen0.ShowImage(screen0.pbCardAnim);
                    ShowScreen(3, 4);
                }
                else if (iMode == 44)
                {
                    CardReader.Reset();
                    CenCom.bCardRequest = true;
                    if (CenCom.bSpanish)
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insertar tarjeta de d" + Convert.ToChar(233) + "bito o presionar CANCEL para usar efectivo en otra terminal");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insert debit card or press CANCEL to use cash at another terminal");
                    }
                    screen0.SetCardAnimLocation(336);
                    //screen0.HideImage(screen0.pbCashAnim);
                    //screen0.ShowImage(screen0.pbCardAnim);
                    ShowScreen(3, 44);
                }
                else if (iMode == 5)
                {
                    CenCom.bBillRequest = true;//placed here to allow more bills after bill returned
                    if (CenCom.bSpanish)
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insertar efectivo o presionar CANCEL para usar tarjeta en otra terminal");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insert cash or press CANCEL to use a card at another terminal");
                    }
                    screen0.SetCashAnimLocation(247);
                    //screen0.ShowImage(screen0.pbCashAnim);
                    //screen0.HideImage(screen0.pbCardAnim);
                    ShowScreen(3, 5);
                }
                else if (iMode == 55)
                {
                    CenCom.bBillRequest = true;//placed here to allow more bills after bill returned
                    if (CenCom.bSpanish)
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insertar efectivo o presionar CANCEL para usar tarjeta de d" + Convert.ToChar(233) + "bito en otra terminal");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insert cash or press CANCEL to use a debit card at another terminal");
                    }
                    screen0.SetCashAnimLocation(247);
                    //screen0.ShowImage(screen0.pbCashAnim);
                    //screen0.HideImage(screen0.pbCardAnim);
                    ShowScreen(3, 55);
                }
                else if (iMode == 6)
                {
                    CenCom.bBillRequest = true;//placed here to allow more bills after bill returned
                    if (CenCom.bSpanish)
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insertar efectivo\nEl lector de tarjeta esta temporalmente fuera de servicio.");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Insert cash\nCard reader is temporarily unavailable.");
                    }
                    screen0.SetCashAnimLocation(247);
                    //screen0.ShowImage(screen0.pbCashAnim);
                    //screen0.HideImage(screen0.pbCardAnim);
                    ShowScreen(3, 6);
                }
                else if (iMode == 7)
                {
                    CenCom.bCashTransaction = true;

                    CardReader.Reset();
                    CenCom.bCardRequest = true;
                    if (CenCom.bSpanish)
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Por favor reinsertar la tarjeta");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lPromptTop, "Please re-insert card");
                    }
                    screen0.SetCardAnimLocation(336);
                    //screen0.HideImage(screen0.pbCashAnim);
                    //screen0.ShowImage(screen0.pbCardAnim);
                    ShowScreen(3, 7);
                }
            }
            else if (iNextScreen == 7)
            {
                CenCom.bBillRequest = true;//placed here to allow more bills after bill returned
                ShowScreen(7, 0);
            }
            else if (iNextScreen == 8)
            {
                screen0.SetText(Display.screen0.lPromptBottom, "");

                if (iMode == 2)//PD- would you like to pump gas or get receipt
                {
                    CenCom.iScreenTimer = 20;

                    if (CenCom.bSpanish)//PD- set here to doesn't get cleared by default (a few statements above)
                    {
                        screen0.SetText(Display.screen0.lPromptBottom, "Bomba " + CenCom.sPump + " seleccionada.");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lPromptBottom, "Pump " + CenCom.sPump + " selected.");
                    }
                }

                ShowScreen(8, 0);
            }
            else if (iNextScreen == 9)
            {
                iMinInput = 4;
                iMaxInput = 12;
                if (CenCom.bSpanish && CenCom.bTestMode == false)
                {
                    screen0.SetText(Display.screen0.lPromptTop, "Marcar n" + Convert.ToChar(250) + "mero de clave de su tarjeta.");
                    if (EPP.iType == 2)
                    {
                        screen0.SetText(Display.screen0.lPromptBottom, "Presionar OK al terminar.");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lPromptBottom, "Presionar ENTER al terminar.");
                    }
                }
                else
                {
                    screen0.SetText(Display.screen0.lPromptTop, "Enter PIN");
                    if (EPP.iType == 2)
                    {
                        screen0.SetText(Display.screen0.lPromptBottom, "Please protect your PIN.\nPress OK when done");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lPromptBottom, "Please protect your PIN.\nPress ENTER when done");
                    }
                }
                //CenCom.sInput = "";//PD- Rev 15 - don't init here in case hit espanol button, init at screen 1, 20, 27 (careful w/ timeout) screen 0 not used because screen 9 won't appear if offline
                screen0.SetText(Display.screen0.lInput, CenCom.sPINEntry);
                ShowScreen(9, 0);
            }
            else if (iNextScreen == 14)
            {
                ShowScreen(14, 0);
            }
            else if (iNextScreen == 20)
            {
                ShowScreen(20, 0);
            }
            else if (iNextScreen == 21)
            {
                //CenCom.sPINEntry = "";//PD- rev 15

                CenCom.iScreenTimer = 60;

                if (iMode == 1)
                {
                    //if (CenCom.iMonitoringLevel == 1)
                    //{
                    //    screen0.SetText(Display.screen0.lMenu3, "7 - Encryption\n\n8 - Monitoring Low");
                    //}
                    //else if (CenCom.iMonitoringLevel == 2)
                    //{
                    //    screen0.SetText(Display.screen0.lMenu3, "7 - Encryption\n\n8 - Monitoring High");
                    //}
                    //else
                    //{
                    //    screen0.SetText(Display.screen0.lMenu3, "7 - Encryption\n\n8 - Monitoring Off");
                    //}
                    //screen0.SetText(Display.screen0.lMenu1, "1 - Disable Alarm\n\n2 - System Test\n\n3 - PIC ID Settings");
                    //screen0.SetText(Display.screen0.lMenu2, "4 - Printer Test\n\n5 - CR Test");
                    //screen0.SetText(Display.screen0.lMenu3, "6 - CA Test\n\n7 - Encryption");
                    screen0.SetText(Display.screen0.lMenu1, "1 - Disable Alarm\n\n4 - Printer Test\n\n7 - Encryption");
                    screen0.SetText(Display.screen0.lMenu2, "2 - System Test\n\n5 - CR Test\n\n8 - Restart");
                    if (CenCom.bLogMaster)
                    {
                        screen0.SetText(Display.screen0.lMenu3, "3 - PIC ID Settings\n\n6 - CA Test\n\n9 - Logging");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lMenu3, "3 - PIC ID Settings\n\n6 - CA Test\n\n9 - Other");
                    }
                    screen0.SetText(Display.screen0.lMsgBottom, "Select action, press CANCEL when done." + "\n" + "PIC ID: " + CenCom.sPICID + "             " + CenCom.AlarmStatus());
                    screen0.SetText(Display.screen0.lVersion, "Version: " + FileAccess.sVersion);
                }
                else if (iMode == 2)
                {
                    screen0.SetText(Display.screen0.lMenu1, "1 - Disable Alarm\n\n4 - Printer Test");
                    screen0.SetText(Display.screen0.lMenu2, "2 - System Test");
                    screen0.SetText(Display.screen0.lMenu3, "3 - Armored Report");
                    screen0.SetText(Display.screen0.lMsgBottom, "Select action, press CANCEL when done." + "\n" + "PIC ID: " + CenCom.sPICID + "             " + CenCom.AlarmStatus());
                    screen0.SetText(Display.screen0.lVersion, "Version: " + FileAccess.sVersion);
                }
                else if (iMode == 3)//PD- rev20
                {
                    screen0.SetText(Display.screen0.lMenu1, "1 - Disable Alarm");
                    screen0.SetText(Display.screen0.lMenu2, "2 - Update Code");
                    if (CenCom.bLogMaster)
                    {
                        screen0.SetText(Display.screen0.lMenu3, "3 - Download Logs");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lMenu3, "");
                    }
                    screen0.SetText(Display.screen0.lMsgBottom, "Select action, press CANCEL when done." + "\n" + "PIC ID: " + CenCom.sPICID + "             " + CenCom.AlarmStatus());
                    screen0.SetText(Display.screen0.lVersion, "Version: " + FileAccess.sVersion);
                }
                else
                {
                    if (Printer.iType > 1)
                    {
                        screen0.SetText(Display.screen0.lMenu1, "1 - Disable Alarm");
                        screen0.SetText(Display.screen0.lMenu2, "2 - System Test");
                        screen0.SetText(Display.screen0.lMenu3, "3 - Printer Test");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lMenu1, "1 - System Test");
                        screen0.SetText(Display.screen0.lMenu2, "2 - Printer Test");
                        screen0.SetText(Display.screen0.lMenu3, "");
                    }
                    screen0.SetText(Display.screen0.lMsgBottom, "Select action, press CANCEL when done." + "\n" + "PIC ID: " + CenCom.sPICID + "             " + CenCom.AlarmStatus());
                    screen0.SetText(Display.screen0.lVersion, "Version: " + FileAccess.sVersion);
                }
                ShowScreen(21, 0);
            }
            //else if (iNextScreen == 22)
            //{
            //    screen22.SetText1(CenCom.sStatus);
            //    ShowScreen(22);
            //}
            else if (iNextScreen == 25)
            {
                screen0.SetText(Display.screen0.lPromptTop, "Please swipe card.");
                screen0.SetText(Display.screen0.lData, "Track 1:\nTrack 2:");
                ShowScreen(25, 0);
            }
            else if (iNextScreen == 26)
            {
                screen0.SetText(Display.screen0.lPromptTop, "Please insert bill.");
                screen0.SetText(Display.screen0.lData, "data");
                ShowScreen(26, 0);
            }
            else if (iNextScreen == 27)
            {
                //CenCom.sPINEntry = "";//PD- rev 15

                if (CenCom.bPINGenerated == false)
                {
                    screen0.SetText(Display.screen0.lMenu1, "1 - Generate test PIN block");
                }
                else
                {
                    screen0.SetText(Display.screen0.lMenu1, "1 - Press CANCEL to try again");
                }

                CenCom.iTempEncrptionType = EPP.iEncryptionMethod;

                if (EPP.iEncryptionMethod == 0)
                {
                    screen0.SetText(Display.screen0.lMenu2, "2 - Switch from SDES to TDES");
                }
                else
                {
                    screen0.SetText(Display.screen0.lMenu2, "2 - Switch from TDES to SDES");
                }
                screen0.SetText(Display.screen0.lData, "data");
                ShowScreen(27, 0);
            }
            else if (iNextScreen == 28)
            {
                ShowScreen(28, 0);
            }
            else if (iNextScreen == 29)
            {
                if (CenCom.bLogMaster)
                {
                    CenCom.iTempLoggingSSC = CenCom.iLoggingSSC;
                    CenCom.iTempLoggingEPP = CenCom.iLoggingEPP;
                    CenCom.iTempLoggingCA = CenCom.iLoggingCA;
                    CenCom.iTempLoggingCR = CenCom.iLoggingCR;
                    CenCom.iTempLoggingPR = CenCom.iLoggingPR;

                    if (CenCom.iLoggingSSC == 0)
                    {
                        screen0.SetText(Display.screen0.lMenu1, "1 - SSC Logging OFF");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lMenu1, "1 - SSC Logging ON");
                    }
                    if (CenCom.iLoggingEPP == 0)
                    {
                        screen0.SetText(Display.screen0.lMenu2, "2 - EPP Logging OFF");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lMenu2, "2 - EPP Logging ON");
                    }
                    if (CenCom.iLoggingCA == 0)
                    {
                        screen0.SetText(Display.screen0.lMenu3, "3 - CA Logging OFF");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lMenu3, "3 - CA Logging ON");
                    }
                    if (CenCom.iLoggingCR == 0)
                    {
                        //CE screen29.SetText4("4 - CR Logging OFF");
                    }
                    else
                    {
                        //CE screen29.SetText4("4 - CR Logging ON");
                    }
                    if (CenCom.iLoggingPR == 0)
                    {
                        //CE screen29.SetText5("5 - PR Logging OFF");
                    }
                    else
                    {
                        //CE screen29.SetText5("5 - PR Logging ON");
                    }
                }
                else
                {
                    CenCom.iTempBrand = CenCom.iBrand;

                    if (CenCom.iBrand == 2)
                    {
                        screen0.SetText(Display.screen0.lMenu1, "1 - BRAND: Thrifty");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lMenu1, "1 - BRAND: ARCO");
                    }

                    CenCom.iTempPtrType = Printer.iType;

                    if (CenCom.iTempPtrType == 4)
                    {
                        screen0.SetText(Display.screen0.lMenu2, "2 - PRINTER: H3 Custom");
                    } 
                    else if (CenCom.iTempPtrType == 3)
                    {
                        screen0.SetText(Display.screen0.lMenu2, "2 - PRINTER: H3 USB");
                    }
                    else if (CenCom.iTempPtrType == 2)
                    {
                        screen0.SetText(Display.screen0.lMenu2, "2 - PRINTER: H3 RS232");
                    }
                    else
                    {
                        screen0.SetText(Display.screen0.lMenu2, "2 - PRINTER: T2 RS232");
                    }

                    screen0.SetText(Display.screen0.lMenu3, "");
                    //CE screen29.SetText4("");
                    //CE screen29.SetText5("");
                }

                ShowScreen(29, 0);
            }
            else if (iNextScreen == 39)
            {
                CenCom.bBillRequest = true;//placed here to allow more bills after bill returned
                CenCom.iScreenTimer = 30;
                ShowScreen(39, 0);
            }
            //fOld.HideThis();
        }

        public static void ShowScreen(int iScreen, int iMode)
        {

            //screen0.ShowImage(screen0.pbBackground);

            screen0.HideImage(screen0.pbLogo1);
            screen0.HideImage(screen0.pbLogo2);
            screen0.HideText(screen0.lChoices);
            screen0.HideText(screen0.lPrompt);
            screen0.HideText(screen0.lPromptBottom);
            screen0.HideText(screen0.lPromptChoices);
            screen0.HideText(screen0.lPromptTop);
            screen0.HideText(screen0.lVersion);
            screen0.HideText(screen0.lInput);
            screen0.HideText(screen0.lInput2);
            screen0.HideText(screen0.lMenu1);
            screen0.HideText(screen0.lMenu2);
            screen0.HideText(screen0.lMenu3);
            screen0.HideText(screen0.lMsg);
            screen0.HideText(screen0.lData);
            screen0.HideText(screen0.lMsgBottom);
            screen0.HideButton(screen0.Cancel);
            screen0.HideButton(screen0.Getreceipt);
            screen0.HideButton(screen0.Pumpgas);
            screen0.HideImage(screen0.pbCashAnim);
            screen0.HideImage(screen0.pbCardAnim);
            screen0.HideButton(screen0.button11);
            screen0.HideButton(screen0.button13);



            if (iScreen == 0)
            {
                screen0.ShowText(screen0.lPrompt);
                screen0.ShowText(screen0.lInput);
                screen0.HideImage(screen0.pbLogo1);
                screen0.HideImage(screen0.pbLogo2);
                ShowKeypad();
            }
            else if (iScreen == 1)
            {
                ShowKeypad();
                screen0.ShowText(screen0.lPromptTop);
                if (CenCom.iBrand == 2)
                {
                    screen0.HideImage(screen0.pbLogo1);
                    screen0.ShowImage(screen0.pbLogo2);
                }
                else
                {
                    screen0.HideImage(screen0.pbLogo2);
                    screen0.ShowImage(screen0.pbLogo1);
                }
            }
            else if (iScreen == 2)
            {
                ShowKeypad();
                screen0.ShowText(screen0.lPrompt);
                screen0.ShowText(screen0.lInput2);
                screen0.HideImage(screen0.pbLogo1);
                screen0.HideImage(screen0.pbLogo2);
            }
            else if (iScreen == 3)
            {
                screen0.ShowText(screen0.lPromptTop);
                screen0.ShowText(screen0.lPromptBottom);
 
                
                if (iMode == 2 || iMode == 22)
                {
                    screen0.ShowImage(screen0.pbCashAnim);
                    screen0.HideText(screen0.lPromptBottom);
                    if (CenCom.inputType == 2)
                    {
                        screen0.ShowButton(screen0.Cancel);
                    }
                    //screen0.ShowImage(screen0.pbCardAnim);
                }
                else if (iMode == 0 || iMode == 5 || iMode == 55 || iMode == 6)
                {
                    screen0.ShowImage(screen0.pbCashAnim);
                    screen0.HideImage(screen0.pbCardAnim);
                }
                else if (iMode == 1 || iMode == 1 || iMode == 4 || iMode == 44 || iMode == 7)
                {
                    screen0.HideImage(screen0.pbCashAnim);
                    screen0.ShowButton(screen0.Cancel);
                    screen0.ShowButton(screen0.button11);
                    //screen0.ShowImage(screen0.pbCardAnim);
                }
                else//for safety
                {
                    screen0.HideImage(screen0.pbCashAnim);
                    screen0.HideImage(screen0.pbCardAnim);
                }
            }
            else if (iScreen == 7) //Dollar Accepted screen
            {
                screen0.ShowText(screen0.lPromptTop);
                screen0.ShowText(screen0.lPromptBottom);
                screen0.ShowText(screen0.lInput);
                screen0.ShowImage(screen0.pbCashAnim);
                screen0.ShowButton(screen0.Cancel);
                screen0.ShowButton(screen0.button13);

            }
            else if (iScreen == 8)//Pump gas or receipt choices
            {
                screen0.ShowText(screen0.lPromptChoices);
                screen0.ShowText(screen0.lChoices);
                screen0.ShowText(screen0.lPromptBottom);//CE- prompt cleared and only shows pump num selected.
                HideKeypad();
                screen0.ShowButton(screen0.Cancel);
                screen0.ShowButton(screen0.Pumpgas);
                screen0.ShowButton(screen0.Getreceipt);
            }
            else if (iScreen == 9)
            {
                screen0.ShowButton(screen0.Cancel);
                screen0.ShowText(screen0.lPromptTop);
                screen0.ShowText(screen0.lPromptBottom);
                screen0.ShowText(screen0.lInput);
                screen0.ShowImage(screen0.pbCashAnim);
                screen0.ShowButton(screen0.Cancel);
                screen0.ShowButton(screen0.Pumpgas);

            }
            else if (iScreen == 14)
            {
                screen0.ShowText(screen0.lMsg);
            }
            else if (iScreen == 20)
            {
                screen0.ShowText(screen0.lMsg);
            }
            else if (iScreen == 21)
            {
                screen0.ShowText(screen0.lMenu1);
                screen0.ShowText(screen0.lMenu2);
                screen0.ShowText(screen0.lMenu3);
                screen0.ShowText(screen0.lMsgBottom);
                screen0.ShowText(screen0.lVersion);
                ShowKeypad();
            }
            else if (iScreen == 25)
            {
                screen0.ShowText(screen0.lPromptTop);
                screen0.ShowText(screen0.lData);
            }
            else if (iScreen == 26)
            {
                screen0.ShowText(screen0.lPromptTop);
                screen0.ShowText(screen0.lData);
                HideKeypad();
                screen0.ShowButton(screen0.Cancel);
            }
            else if (iScreen == 27)
            {
                screen0.ShowText(screen0.lMenu1);
                screen0.ShowText(screen0.lMenu2);
                screen0.ShowText(screen0.lData);
            }
            else if (iScreen == 29)
            {
                screen0.ShowText(screen0.lMenu1);
                screen0.ShowText(screen0.lMenu2);
                screen0.ShowText(screen0.lMenu3);
            }
            else if (iScreen == 39)
            {
                screen0.ShowText(screen0.lMsgLeft);
                screen0.ShowText(screen0.lMsgRight);
                screen0.ShowText(screen0.lPromptBottom);
            }
            else if (iScreen == 40)
            {
                screen0.ShowText(screen0.lPromptTop);
            }

            screen0.ShowThis();
        }

        public static void ShowMessageBox(string sText, int iTimer)
        {
            //bMsgBoxShowing = true;

            CenCom.iMsgBoxTimer = iTimer;

            screen0.SetText(screen0.lMsgBox, sText);

            screen0.ShowImage(screen0.pbMsgBox);
            screen0.ShowText(screen0.lMsgBox);
            screen0.ShowThis();

            Debug.WriteLine("ShowMessageBox: " + sText);
            Debug.WriteLine(CenCom.iMsgBoxTimer);
        }

        public static void HideMessageBox()
        {
            Debug.WriteLine("HideMessageBox");
            bMsgBoxShowing = false;

            screen0.SetText(screen0.lMsgBox, "");

            screen0.HideText(screen0.lMsgBox);
            screen0.HideImage(screen0.pbMsgBox);
            screen0.ShowThis();

            //myMsgBox.HideThis();
        }
    }

    public static class EPP
    {
        public static int iPortNum=0;
        public static string myData;
        static int count = 0;//for debugging

        public static int iType = 1;
        public static int iInit = 0;//Fujitsu

        public static int iBytesRead = 0;
        public static int iLength = 0;
        public static int iCmd = 0;
        public static int iResult = 0;
        public static bool bProcessPIN = false;

        public static SerialPort PortEPP = new SerialPort();
        
        public static bool bWaitingForResponse = false;
        public static bool bGeneratedPINBlock = false;
        public static int iStatus = 0;
        public static int iEncryptionMethod=0;
        public static string EPB = "";
        public static string KSN = "";
        public static bool bEncryptionEnabled = false;

        static bool bTriggerPINBlock = false;

        static byte bPreviousByte = 0xFF;//Fujitsu
        static bool bACK = false;

        public static void Init()
        {
            PortEPP.BaudRate = 38400;
            PortEPP.StopBits = StopBits.One;
            if (iType == 2)//Fujitus
            {
                PortEPP.Parity = Parity.Even;
            }
            else
            {
                PortEPP.Parity = Parity.None;
            }
            PortEPP.DataBits = 8;
            PortEPP.PortName = "COM" + iPortNum;
            PortEPP.Handshake = Handshake.None;

            PortEPP.ReadTimeout = 1000;//may need to change...

            PortEPP.DataReceived += new SerialDataReceivedEventHandler(PortEPP_DataReceived);

            try
            {
                PortEPP.Open();

                if (iType == 2)//Fujitsu
                {
                    Debug.WriteLine("fujitsu startup........");
                    FujitsuStartup();
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                Debug.WriteLine(ex.Message);
            }

            //if (PortEPP.IsOpen == true)
            //{
            //    try
            //    {
            //        PortEPP.Close();
            //    }
            //    catch (Exception ex)
            //    {
            //        Debug.WriteLine(ex.Message);
            //    }
            //}
            //if (PortEPP.IsOpen == false)
            //{
            //    PortEPP.Dispose();

            //    PortEPP.BaudRate = 38400;
            //    PortEPP.StopBits = StopBits.One;
            //    PortEPP.Parity = Parity.None;
            //    PortEPP.DataBits = 8;
            //    PortEPP.PortName = "COM" + iPortNum;
            //    PortEPP.Handshake = Handshake.None;

            //    PortEPP.DataReceived += new SerialDataReceivedEventHandler(PortEPP_DataReceived);
            //    //PortEPP.ErrorReceived += new
            //    //SerialErrorReceivedEventHandler(ErrorInSerialCommunication);

            //    // PortEPP.ReadBufferSize = 7;

            //    try
            //    {
            //        PortEPP.Open();
            //    }
            //    catch (Exception ex)
            //    {
            //        Debug.WriteLine(ex.Message);
            //    }
            //}
        }

        public static void FujitsuStartup()
        {
            int i;
            int iLength = 0;

            byte[] bSend = new byte[99];
            byte[] bSend1 = new byte[] { 0x02, 0x32, 0x71, 0x3C, 0x70, 0x30, 0x70, 0x37, 0x70, 0x31, 0x7C, 0x03, 0xBA };
            byte[] bSend2 = new byte[] { 0x02, 0x33, 0x72, 0x3C, 0x70, 0x36, 0x70, 0x37, 0x75, 0x30, 0x70, 0x30, 0x75, 0x37, 0x7D, 0x34, 0x72, 0x30, 0x71, 0x30, 0x7C, 0x30, 0x70, 0x31, 0x7C, 0x03, 0x21 };
            byte[] bSend3 = new byte[] { 0x02, 0x34, 0x73, 0x3C, 0x70, 0x36, 0x70, 0x3D, 0x7D, 0x30, 0x70, 0x32, 0x76, 0x31, 0x73, 0x32, 0x74, 0x33, 0x75, 0x34, 0x76, 0x30, 0x70, 0x38, 0x71, 0x34, 0x72, 0x35, 0x73, 0x36, 0x70, 0x37, 0x79, 0x31, 0x70, 0x31, 0x73, 0x32, 0x74, 0x33, 0x75, 0x34, 0x76, 0x30, 0x70, 0x38, 0x71, 0x34, 0x72, 0x35, 0x73, 0x36, 0x70, 0x37, 0x79, 0x30, 0x70, 0x31, 0x73, 0x32, 0x74, 0x33, 0x75, 0x34, 0x76, 0x30, 0x70, 0x38, 0x71, 0x34, 0x72, 0x35, 0x73, 0x36, 0x70, 0x37, 0x79, 0x3C, 0x70, 0x38, 0x71, 0x34, 0x72, 0x35, 0x73, 0x36, 0x70, 0x37, 0x79, 0x31, 0x7C, 0x03, 0x80 };
            byte[] bSend4 = new byte[] { 0x02, 0x35, 0x74, 0x3C, 0x70, 0x36, 0x70, 0x37, 0x72, 0x30, 0x70, 0x30, 0x71, 0x30, 0x71, 0x31, 0x7C, 0x03, 0xCA };
            byte[] bSend5 = new byte[] { 0x02, 0x37, 0x76, 0x30, 0x72, 0x36, 0x70, 0x33, 0x7A, 0x30, 0x70, 0x30, 0x71, 0x30, 0x70, 0x31, 0x7C, 0x03, 0xCD };

            if (iInit == 0)
            {
                iInit = 1;
                bSend = bSend1;
                iLength = 13;
                Debug.WriteLine("init 1");
            }
            else if (iInit == 1)
            {
                iInit = 2;
                bSend = bSend2;
                iLength = 27;
                Debug.WriteLine("init 2");
            }
            else if (iInit == 2)
            {
                iInit = 3;
                bSend = bSend3;
                iLength = 93;
                Debug.WriteLine("init 3");
            }
            else if (iInit == 3)
            {
                iInit = 4;
                bSend = bSend4;
                iLength = 19;
                Debug.WriteLine("init 4");
            }
            else if (iInit == 4)
            {
                iInit = 0;
                bSend = bSend5;
                iLength = 19;
                Debug.WriteLine("init 5");
            }

            Debug.WriteLine("*******************************Fujitsu");
            try
            {
                if (PortEPP.IsOpen == false)
                {
                    PortEPP.Open();

                    Debug.WriteLine("EPP Port not opened... Trying to open port...");
                }
                else
                {
                    PortEPP.DiscardOutBuffer();
                    PortEPP.Write(bSend, 0, iLength);
                    if (CenCom.iLoggingEPP == 1)
                    {
                        WriteLogData_TX(Environment.NewLine + "TX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                        for (i = 0; i < iLength; i++)
                        {
                            WriteLogData_TX(String.Format("{0:X2}", bSend[i]) + " ");
                        }
                    }
                    bEncryptionEnabled = false;

                    Debug.WriteLine("Initializing EPP");
                }
                //return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("EPP ERROR: ");
                Debug.WriteLine(ex.Message);
                //return false;
            }
        }

        public static void FujitsuReset()
        {
            int i;
            
            byte[] bSend = new byte[] { 0x02, 0x37, 0x76, 0x30, 0x72, 0x36, 0x70, 0x33, 0x7A, 0x30, 0x70, 0x30, 0x71, 0x30, 0x70, 0x31, 0x7C, 0x03, 0xCD };
        
            Debug.WriteLine("*******************************Fujitsu");
            try
            {
                if (PortEPP.IsOpen == false)
                {
                    PortEPP.Open();

                    Debug.WriteLine("EPP Port not opened... Trying to open port...");
                }
                else
                {
                    PortEPP.DiscardOutBuffer();
                    PortEPP.Write(bSend, 0, 19);
                    if (CenCom.iLoggingEPP == 1)
                    {
                        WriteLogData_TX(Environment.NewLine + "TX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                        for (i = 0; i < 19; i++)
                        {
                            WriteLogData_TX(String.Format("{0:X2}", bSend[i]) + " ");
                        }
                    }
                    bEncryptionEnabled = false;

                    Debug.WriteLine("Resetting EPP");
                }
                //return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("EPP ERROR: ");
                Debug.WriteLine(ex.Message);
                //return false;
            }
        }

        private static void FujitsuSendACK()
        {
            int i;
            byte[] bSend = new byte[] {6};

            Debug.WriteLine("Send ACK");
            try
            {
                if (PortEPP.IsOpen == false)
                {
                    PortEPP.Open();

                    Debug.WriteLine("EPP Port not opened... Trying to open port...");
                }
                else
                {
                    PortEPP.DiscardOutBuffer();
                    PortEPP.Write(bSend, 0, 1);
                    if (CenCom.iLoggingEPP == 1)
                    {
                        WriteLogData_TX(Environment.NewLine + "TX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                        for (i = 0; i < iLength; i++)
                        {
                            WriteLogData_TX(String.Format("{0:X2}", bSend[i]) + " ");
                        }
                    }
                    bEncryptionEnabled = false;

                    Debug.WriteLine("ACK Sent");
                }
                //return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("EPP ERROR: ");
                Debug.WriteLine(ex.Message);
                //return false;
            }
        }

        private static void PortEPP_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int iBytesToRead = PortEPP.BytesToRead;
            int i;
            byte[] comBuffer = new byte[iBytesToRead];
            byte bCurrentByte;

            Debug.WriteLine("EPP Data Received");
            Debug.WriteLine("EPP bytes to read: " + iBytesToRead);

            try
            {
                if (CenCom.iLoggingEPP == 1)
                {
                    WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                }
            }
            catch
            {

            }

            if (iStatus < 1) 
            { 
                iStatus = 1;
                Debug.WriteLine("EPP Online");
            }

            if (bWaitingForResponse == true)//if get ANY response link is up
            {
                bWaitingForResponse=false;
                Debug.WriteLine("EPP WaitingForResponse Off");
            }

            //*** PROBLEM LENGTH AND BCC COULD = STX AND ETX

            try
            {
                PortEPP.Read(comBuffer, 0, iBytesToRead);
            }
            catch
            {
            }
            for (i = 0; i < iBytesToRead; i++)
            {
                try
                {
                    if (CenCom.iLoggingEPP == 1)
                    {
                        WriteLogData_RX(String.Format("{0:X2}", comBuffer[i]) + " ");
                    }
                }
                catch
                {

                }

                if (iType == 2)
                {
                        if (comBuffer[i] == 3)
                        {
                            bACK = true;
                        }
                        else if (bACK)
                        {

                            bACK = false;
                            FujitsuSendACK();
                            if (iInit > 0) { FujitsuStartup(); }
                        }
                        else if (iBytesRead == 0 && comBuffer[i] == 2)
                        {
                            iBytesRead++;
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            Debug.WriteLine("EPP SS");
                        }
                        else if (iBytesRead == 1)
                        {
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else if (iBytesRead == 2)
                        {
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else if (iBytesRead == 3 && comBuffer[i] == 0x70)
                        {
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else if (iBytesRead == 4 && comBuffer[i] == 0xF2)
                        {
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else if (iBytesRead == 5 && comBuffer[i] == 0x7E)
                        {
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else if (iBytesRead == 6 && comBuffer[i] == 0xF4)
                        {
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else if (iBytesRead == 7 && comBuffer[i] == 0x73)
                        {
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else if (iBytesRead == 8 && comBuffer[i] == 0xFA)
                        {
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else if (iBytesRead == 9 && comBuffer[i] == 0x70)
                        {
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else if (iBytesRead == 10 && comBuffer[i] == 0xF0)
                        {
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else if (iBytesRead == 11 && comBuffer[i] == 0x70)
                        {
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else if (iBytesRead == 12 && comBuffer[i] == 0xF4)
                        {
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else if (iBytesRead == 13 && comBuffer[i] == 0x71)
                        {
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else if (iBytesRead == 14 && comBuffer[i] == 0xFA)
                        {
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else if (iBytesRead == 15 && comBuffer[i] == 0x70)
                        {
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else if (iBytesRead == 16 && comBuffer[i] == 0xF0)
                        {
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else if (iBytesRead == 17)//sum
                        {
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else if (iBytesRead == 18)
                        {
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else if (iBytesRead == 19)//sum
                        {
                            bPreviousByte = comBuffer[i];

                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else if (iBytesRead == 20)
                        {
                            if (bPreviousByte == 0x73)
                            {
                                GetKey(comBuffer[i] ^ 0xF0);
                            }
                            else if (bPreviousByte == 0x70 && comBuffer[i] == 0xFD)
                            {
                                GetKey(15);
                            }
                            else if (bPreviousByte == 0x70 && comBuffer[i] == 0xF8)
                            {
                                GetKey(14);
                            }
                            else if (bPreviousByte == 0x71 && comBuffer[i] == 0xFB)
                            {
                                GetKey(13);
                            }
                            else if (bPreviousByte == 0x74 && comBuffer[i] == 0xF2)
                            {
                                GetKey(12);
                            }

                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            iBytesRead++;
                        }
                        else
                        {
                            if (comBuffer[i] == 6)
                            {
                                //if (iInit > 0)
                                //{
                                //    FujitsuStartup();
                                //}
                                Debug.WriteLine("ACK");
                                Debug.WriteLine("iBytesRead: " + iBytesRead);
                            }
                            else
                            {
                                Debug.WriteLine("iBytesRead RESET ERROR");
                                Debug.WriteLine("iBytesRead: " + iBytesRead);
                                Debug.WriteLine("comBuffer[i]: " + comBuffer[i]);
                                iBytesRead = 0;
                                iLength = 0;
                                iCmd = 0;
                                iResult = 0;
                                bProcessPIN = false;
                            }
                        }
                }
                else
                {
                    if (iBytesRead == 0 && comBuffer[i] == 2)
                    {
                        iBytesRead++;
                        Debug.WriteLine("iBytesRead: " + iBytesRead);
                        Debug.WriteLine("EPP SS");
                    }
                    else if (iBytesRead == 1)
                    {
                        iBytesRead++;
                        Debug.WriteLine("iBytesRead: " + iBytesRead);
                        iLength = comBuffer[i];
                        Debug.WriteLine("iLength: " + iLength);
                    }
                    else if (iBytesRead == 2)
                    {
                        iBytesRead++;
                        Debug.WriteLine("iBytesRead: " + iBytesRead);
                        Debug.WriteLine("comBuffer[i]: " + comBuffer[i]);
                    }
                    else if (iBytesRead == 3)//get cmd
                    {
                        iBytesRead++;
                        Debug.WriteLine("iBytesRead: " + iBytesRead);
                        iCmd = comBuffer[i];
                        Debug.WriteLine("iCmd= " + iCmd);
                    }
                    else if (iBytesRead == 4)//get result
                    {
                        iBytesRead++;
                        Debug.WriteLine("iBytesRead: " + iBytesRead);
                        iResult = comBuffer[i];
                        Debug.WriteLine("iResult= " + iResult);

                        if ((iCmd == 48 || iCmd == 49) && iLength == 2)
                        {
                            GetKey(iResult);
                        }
                        else if (iCmd == 145)
                        {
                            if (iResult == 79)
                            {
                                Debug.WriteLine("Enable Encryption Feedback Success");

                                bTriggerPINBlock = true;

                                //if (CenCom.bTestMode == false)
                                //{
                                //    //bGeneratePINBlock = false;
                                //    EPP.bGeneratedPINBlock = EPP.GeneratePINBlock(EPP.iEncryptionMethod, CardReader.sPAN);
                                //}
                                //else
                                //{
                                //    //bGeneratePINBlock = false;
                                //    EPP.bGeneratedPINBlock = EPP.GeneratePINBlock(EPP.iEncryptionMethod, FileAccess.GetTestPAN());
                                //    Display.GotoScreen(27, 0);
                                //}
                            }
                            else
                            {
                                if (CenCom.bTestMode == true)
                                {
                                    Display.GotoScreen(27, 0);
                                    Display.screen0.SetText(Display.screen0.lData, "Could not re-enable encryption mode.");
                                }
                                else
                                {
                                    RS485.SendCancel();
                                }

                                EPP.Initialize();
                                Debug.WriteLine("Enable Encryption Feedback Failure");
                            }
                        }
                        else if (iCmd == 161)
                        {
                            if (iResult == 79 && iLength == 20)
                            {
                                EPB = "";
                                KSN = "";
                                bProcessPIN = true;
                                Debug.WriteLine("PIN Block Success");
                            }
                            else
                            {
                                if (CenCom.bTestMode == true)
                                {
                                    Display.GotoScreen(27, 0);
                                    Display.screen0.SetText(Display.screen0.lData, "Failed to generate PIN block.");
                                }
                                else
                                {
                                    RS485.SendCancel();
                                }

                                EPP.Initialize();
                                Debug.WriteLine("PIN Block Failure");
                            }
                        }
                    }
                    else if (iBytesRead >= 5 && iBytesRead < (iLength + 5))
                    {
                        iBytesRead++;
                        Debug.WriteLine("iBytesRead: " + iBytesRead);

                        if (iBytesRead == (iLength + 4))
                        {
                            Debug.WriteLine("etx: " + comBuffer[i]);
                        }
                        else if (iBytesRead == (iLength + 5))
                        {
                            Debug.WriteLine("bcc: " + comBuffer[i]);
                            iBytesRead = 0;
                            iLength = 0;
                            iCmd = 0;
                            iResult = 0;
                            Debug.WriteLine("iBytesRead RESET");

                            if (bTriggerPINBlock)
                            {
                                //bTriggerPINBlock = false;//PD- Placed below as well in case this was missed when it should have been called.

                                if (CenCom.bTestMode == false)
                                {
                                    EPP.bGeneratedPINBlock = EPP.GeneratePINBlock(EPP.iEncryptionMethod, CardReader.sPAN);
                                }
                                else
                                {
                                    EPP.bGeneratedPINBlock = EPP.GeneratePINBlock(EPP.iEncryptionMethod, FileAccess.GetTestPAN());
                                    Display.GotoScreen(27, 0);
                                }
                            }
                            else if (bProcessPIN)
                            {
                                EPP.Initialize();
                                bProcessPIN = false;
                            }
                            bTriggerPINBlock = false;//PD- Placed here in case this was missed when it should have been called. 
                        }
                        else if (bProcessPIN)
                        {
                            //processing....

                            bCurrentByte = comBuffer[i];
                            if (iBytesRead <= 13)
                            {
                                if (bCurrentByte < 16)
                                {
                                    EPB = EPB + "0";
                                }
                                EPB = EPB + String.Format("{0:X}", bCurrentByte);
                                Debug.WriteLine("EPB " + EPB);
                            }
                            else if (iBytesRead <= 23)
                            {
                                if (bCurrentByte < 16)
                                {
                                    KSN = KSN + "0";
                                }
                                KSN = KSN + String.Format("{0:X}", bCurrentByte);
                                Debug.WriteLine("KSN " + KSN);
                                if (iBytesRead == 23)
                                {
                                    if (CenCom.bTestMode == false)
                                    {
                                        RS485.SendPINData();
                                    }
                                    else
                                    {
                                        Display.screen0.SetText(Display.screen0.lData, "EPB: " + EPB + "\nKSN: " + KSN);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Debug.WriteLine("comBuffer[i]: " + comBuffer[i]);
                        }
                    }

                    else
                    {
                        if (comBuffer[i] == 6)
                        {
                            Debug.WriteLine("ACK");
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                        }
                        else
                        {
                            Debug.WriteLine("iBytesRead RESET ERROR");
                            Debug.WriteLine("iBytesRead: " + iBytesRead);
                            Debug.WriteLine("comBuffer[i]: " + comBuffer[i]);
                            iBytesRead = 0;
                            iLength = 0;
                            iCmd = 0;
                            iResult = 0;
                            bProcessPIN = false;
                        }
                    }
                }
            }
        }

        public static void CheckStatus()
        {

            if (CenCom.inputType == 2)
            {
                //Force Touchscreen iStatus to 1
                iStatus = 1;
                bWaitingForResponse = false;
            }
            else
            {

                int i;
                byte[] bSend = new byte[] { 2, 1, 0, 16, 3, 18 };
                bWaitingForResponse = true;

                Debug.WriteLine("*******************************FMI2222");
                try
                {
                    if (PortEPP.IsOpen == false)
                    {
                        PortEPP.Open();

                        Debug.WriteLine("EPP Port not opened... Trying to open port...");
                    }
                    else
                    {
                        PortEPP.DiscardOutBuffer();
                        PortEPP.Write(bSend, 0, 6);
                        if (CenCom.iLoggingEPP == 1)
                        {
                            WriteLogData_TX(Environment.NewLine + "TX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                            for (i = 0; i < 6; i++)
                            {
                                WriteLogData_TX(String.Format("{0:X2}", bSend[i]) + " ");
                            }
                        }
                        bEncryptionEnabled = false;

                        Debug.WriteLine("Checking EPP status", DateTime.Now.ToString("h:mm:ss.fff"));
                        Debug.WriteLine(bSend[0] + "-" + bSend[1] + "-" + bSend[2] + "-" + bSend[3] + "-" + bSend[4] + "-" + bSend[5]);
                    }
                    //return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("EPP ERROR: ");
                    Debug.WriteLine(ex.Message);
                    //return false;
                }
                //try
                //{
                //    if (PortEPP.IsOpen == false)
                //    {
                //        PortEPP.Open();
                //    }
                //    PortEPP.Write(bSend, 0, 6);

                //    bEncryptionEnabled = false;

                //    Debug.WriteLine("Checking EPP status", DateTime.Now.ToString("h:mm:ss.fff"));
                //}
                //catch (Exception ex)
                //{
                //    Debug.WriteLine(ex.Message);
                //}
            }
        }

        public static bool Initialize()
        {
            int i;
            byte[] bSend = new byte[] { 2, 1, 0, 16, 3, 18 };

            Debug.WriteLine("*******************************FMI");
            try
            {
                if (PortEPP.IsOpen == false)
                {
                    PortEPP.Open();

                    Debug.WriteLine("EPP Port not opened... Trying to open port...");
                }
                else
                {
                    PortEPP.DiscardOutBuffer();
                    PortEPP.Write(bSend, 0, 6);
                    if (CenCom.iLoggingEPP == 1)
                    {
                        WriteLogData_TX(Environment.NewLine + "TX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                        for (i = 0; i < 6; i++)
                        {
                            WriteLogData_TX(String.Format("{0:X2}", bSend[i]) + " ");
                        }
                    }
                    bEncryptionEnabled = false;

                    Debug.WriteLine("Initializing EPP");
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("EPP ERROR: ");
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public static bool EnableEncryption(int iMethod)
        {
            //byte[] bSend0=new byte[] {2, 6, 0, 145, 0, 1, 4, 8, 0, 3, 153};
            //byte[] bSend1=new byte[] {2, 6, 0, 145, 1, 1, 4, 8, 0, 3, 152};

            byte[] bSend = new byte[11];
            byte bcc = 0;
            int i;

            bSend[0] = 2;
            bSend[1] = 6;
            bSend[2] = 0;
            bSend[3] = 145;
            bSend[4] = Convert.ToByte(iMethod);
            bSend[5] = 1;
            bSend[6] = 4;
            bSend[7] = 12;
            bSend[8] = 0;
            bSend[9] = 3;

            for (i = 1; i < 10; i++)//skip STX
            {
                bcc = (byte)(bcc ^ bSend[i]);
                //Debug.WriteLine(i);
                //Debug.WriteLine(bcc);
            }
            bSend[10] = bcc;

            try
            {
                if (PortEPP.IsOpen == false)
                {
                    PortEPP.Open();
                }
                PortEPP.Write(bSend, 0, 11);
                if (CenCom.iLoggingEPP == 1)
                {
                    WriteLogData_TX(Environment.NewLine + "TX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                    for (i = 0; i < 11; i++)
                    {
                        WriteLogData_TX(String.Format("{0:X2}", bSend[i]) + " ");
                    }
                }
                bEncryptionEnabled = true;
                Debug.WriteLine("Encryption Enabled: " + iMethod);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: Could not enable encryption. - "+ex.Message);
                return false;
            }
        }

        public static bool GeneratePINBlock(int iMethod, string sPAN)
        {
            //byte[] bSend0 = new byte[] { 2, 8, 0, 161, 0, 64, 18, 52, 86, 120, 144, 3, 114 };
            //byte[] bSend1 = new byte[] { 2, 8, 0, 161, 1, 64, 18, 52, 86, 120, 144, 3, 115 };

            byte[] bSend = new byte[13];
            byte bcc=0;
            int i;

            bSend[0] = 2;
            bSend[1] = 8;
            bSend[2] = 0;
            bSend[3] = 161;
            bSend[4] = Convert.ToByte(iMethod);

            Debug.WriteLine(sPAN);
            for (i = 0; i < 6; i++)
            {
                //Debug.WriteLine((sPAN[i*2]-48));
                //Debug.WriteLine((sPAN[i*2+1]-48)); 
                //Debug.WriteLine( (byte)((sPAN[2 * i] - 48) << 4));
                //Debug.WriteLine( (byte)((sPAN[2 * i +1] - 48)));
                Debug.WriteLine("PAN BYTE: " + i + "=" + (byte)(((sPAN[2 * i] - 48) << 4) + (sPAN[2 * i + 1] - 48)));
                bSend[i + 5] = (byte)(((sPAN[2 * i] - 48) << 4) + (sPAN[2 * i + 1] - 48));
            }
                
            bSend[11]=3;

            for (i = 1; i < 12; i++)//skip STX
            {
                bcc = (byte)(bcc ^ bSend[i]);
                Debug.WriteLine(i);
                Debug.WriteLine(bcc);
            }
            bSend[12] = bcc;

            try
            {
                if (PortEPP.IsOpen == false)
                {
                    PortEPP.Open();
                }

                PortEPP.Write(bSend, 0, 13);
                if (CenCom.iLoggingEPP == 1)
                {
                    WriteLogData_TX(Environment.NewLine + "TX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                    for (i = 0; i < 13; i++)
                    {
                        WriteLogData_TX(String.Format("{0:X2}", bSend[i]) + " ");
                    }
                }
                Debug.WriteLine("Generated PIN Block - iMethod= " + iMethod);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Could not generate PIN block - "+ex.Message);
                return false;
            }
        }

        public static int GetKey(int iKey)
        {
            Debug.WriteLine(Convert.ToString(iKey));

            if (iKey >=0 && iKey <=15 && bEncryptionEnabled==false)//Nums
            {
                CenCom.ProcessKey(iKey);

                Debug.WriteLine("Key "+iKey+ " " + count);
                count++;
                return iKey;
            }
            else if ((iKey == 42 || iKey == 15 || iKey == 14 || iKey == 13 || iKey == 12) && bEncryptionEnabled == true)//Nums
            {
                CenCom.ProcessKey(iKey);

                Debug.WriteLine("Key " + iKey + " " + count);
                count++;
                return iKey;
            }
            else
            {
                return -1;
            }
        }
        static void WriteLogData_RX(string sData)
        {
            try
            {
                if (CenCom.sOS == "CE")
                {
                    FileAccessCE.AppendFile("\\SDCard\\SPT\\LOGS\\KP_RX_" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
                else
                {
                    File.AppendAllText("LOGS\\KP_RX_" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
            }
            catch
            {

            }
        }

        static void WriteLogData_TX(string sData)
        {
            try
            {
                if (CenCom.sOS == "CE")
                {
                    FileAccessCE.AppendFile("\\SDCard\\SPT\\LOGS\\KP_TX_" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
                else
                {
                    File.AppendAllText("LOGS\\KP_TX_" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
            }
            catch
            {

            }
        }
    }

    public static class CardReader
    {
        public static int iPortNum = 0;
        public static string myData;

        static SerialPort PortCR = new SerialPort();

        static byte[] bSend = new byte[] {27,73};

        public static bool bWaitingForResponse = false;
        public static int iStatus = 0;

        public static bool bCardLock = false;
        public static int iCardInsertedTimer = 0;

        static int iTrack = 0;
        static int iPANCount = 0;
        public static string sPAN = "";
        public static string sTrack1 = "";
        public static string sTrack2 = "";
        static string sData = "";
        public static bool bCardInserted = false;
        static bool bCardRead = false;
        static string sRead = "";

        public static void Init()
        {
            PortCR.BaudRate = 9600;
            PortCR.StopBits = StopBits.One;
            PortCR.Parity = Parity.None;
            PortCR.DataBits = 8;
            PortCR.PortName = "COM" + iPortNum;
            PortCR.Handshake = Handshake.None;
            //PortCR.RtsEnable = true;//need to power cr??
            PortCR.DtrEnable = true;//need to power cr??
            //PortCR.ReceivedBytesThreshold = 1;
            PortCR.ReadTimeout = 1000;//may need to change...

            PortCR.DataReceived += new SerialDataReceivedEventHandler(PortCR_DataReceived);

            try
            {
                PortCR.Open();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                Debug.WriteLine(ex.Message);
            }
        }

        static void PortCR_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int bytes = PortCR.BytesToRead;
            int iByteRead;

            Debug.WriteLine("CR Data Received");

            Debug.WriteLine("CR bytes to read: "+bytes);

            try
            {
                if (CenCom.iLoggingCR == 1)
                {
                    WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                }
            }
            catch
            {
            }

            if (iStatus < 1) 
            {
                iStatus = 1;
                Debug.WriteLine("CR Online");
                RS485.SendCRS();
                Debug.WriteLine("Purge CR Online: ");//pd- pass through the initial string sent by the cr
                try
                {
                    sRead = PortCR.ReadExisting();
                    if (CenCom.iLoggingCR == 1)
                    {
                        WriteLogData_RX(sRead);
                    }
                }
                catch
                {
                }
            }

            if (bWaitingForResponse == true)
            {
                bWaitingForResponse = false;
                Debug.WriteLine("CR WaitingForResponse Off");
            }

                for (iTrack = 1; iTrack <= 2; iTrack++)
                {
                    try
                    {
                        iByteRead = PortCR.ReadByte();
                        if (CenCom.iLoggingCR == 1)
                        {
                            if (iByteRead < 48)
                            {
                                WriteLogData_RX(Convert.ToString(iByteRead));
                            }
                            else
                            {
                                WriteLogData_RX(Convert.ToString((char)iByteRead));
                            }
                        }
                        bytes--;
                        Debug.WriteLine("CR BYTE READ1: " + iByteRead);

                        //Debug.WriteLine(iByteRead + "    " + iTrack);
                        if (iByteRead == 49 && iTrack == 1 && bCardInserted == false && iStatus != 2)//1st byte read = 1 -> card inserted...
                        {
                     
                            //Reset();//pd- don't reset track data if swipe on screen 1 (pre-swipe) and then re-swipe right afterwords
                            bCardInserted = true;
                            Debug.WriteLine("Card inserted");

                            iCardInsertedTimer = 0;
                            bCardLock = false;

                            SetStatus(2);//pd- cr status must always be maintained although not always sent to ssc
                            
                            if (Display.iCurrentScreen == 25)
                            {
                                Display.screen0.SetText(Display.screen0.lPromptTop, "Please remove card.");
                            }
                            else
                            {
                                if (CenCom.bCardRequest == true && Display.iCurrentScreen == 3)//not for screen 1, bCardRequest should = true for only first successful swipe
                                {
                                    RS485.SendCRS();
                                }
                                else
                                {
                                    if (CenCom.bSpanish)
                                    {
                                        Display.ShowMessageBox("Retire su tarjeta", 3);
                                    }
                                    else
                                    {
                                        Display.ShowMessageBox("Remove card", 3);
                                    }
                                }

                            }

                            if (bytes > 0)
                            {
                                iByteRead = PortCR.ReadByte();//read next char...
                                if (CenCom.iLoggingCR == 1)
                                {
                                    if (iByteRead < 48)
                                    {
                                        WriteLogData_RX(Convert.ToString(iByteRead));
                                    }
                                    else
                                    {
                                        WriteLogData_RX(Convert.ToString((char)iByteRead));
                                    }
                                }
                                Debug.WriteLine("CR BYTE READ2: " + iByteRead);
                            }
                        }

                            //PD- caused msg to appear on screen 1 if left card in during checkstatus
                        //if (iByteRead == 48 && bCardInserted == true)//pd- for magtek;  otherwise check status will cause this value to be transmitted
                        //{
                        //    bCardInserted = false;
                        //    if (CenCom.bCardRequest == true)
                        //    {
                        //        SetStatus(3);
                        //    }
                        //    else
                        //    {
                        //        SetStatus(1);
                        //    }

                        //    //Reset();//pd- don't reset track data if swipe on screen 1 (pre-swipe) and then re-swipe right afterwords
                        //    if (Display.iCurrentScreen == 1)
                        //    {
                        //        Display.ShowMessageBox("1-Card inserted incorrectly.\n\nRe-insert card.");
                        //    }
                        //    else if (CenCom.bCardRequest == true)
                        //    {
                        //        RS485.SendCRS();
                        //        Display.screen0.SetText(Display.screen0.lMsg, "1-Card inserted incorrectly.\n\nRe-insert card.");
                        //        Display.GotoScreen(20, 0);  
                        //    }
                        //    else if (Display.iCurrentScreen == 25)
                        //    {
                        //        Display.screen0.SetText(Display.screen0.lPromptTop, "Card inserted incorrectly. Re-insert card.");
                        //        Display.screen0.SetText(Display.screen0.lData, "Track 1:\nTrack 2:");
                        //    }
                        //    else
                        //    {
                        //        Display.ShowMessageBox("1-Can't read card now.");
                        //    }
                        //}
                        else if (iByteRead == 48)
                        {
                            //nothing, just a status check...
                            break;
                        }
                        //else if (iByteRead == 49)//GO AHEAD W/ PURGING... SHOULD READ TRACK DATA ON NEXT RECEIVE EVENT...
                        //{
                        //    //probably just card inserted byte set but no other bytes read...
                        //    Debug.WriteLine("MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM");
                        //    break;
                        //}
                        else if (iByteRead == 37 || iByteRead == 59)
                        {
                            //Debug.WriteLine("UUUUUUUUUUUUUUUUUUUUUUUUUUU: " + iByteRead);
                            //bCardRead = true;//pd- set below due to how motorized cr handles no track data
                            bCardInserted = false;
                            if (CenCom.bCardRequest == true && Display.iCurrentScreen != 1)//PD- rev21 - enable successful read on screen 3 if misread on screen 1
                            {
                                SetStatus(3);
                                Debug.WriteLine("zzzzzzzzzzzzzzzzzzzzzzzzzzzCR STATUS SET TO 3");
                            }
                            else
                            {
                                SetStatus(1);
                                Debug.WriteLine("zzzzzzzzzzzzzzzzzzzzzzzzzzzCR STATUS SET TO 1");
                            }

                            if (iByteRead == 37)
                            {
                                iTrack = 1;
                            }
                            else if (iByteRead == 59)
                            {
                                iTrack = 2;
                            }

                            try
                            {
                                sData = PortCR.ReadTo("?");
                                if (sData != "" && CenCom.iLoggingCR == 1)
                                {
                                    WriteLogData_RX(sData + "?");
                                }
                                //Debug.WriteLine("00000000000000000000000000000000: " + sData);
                                if (sData == "E") { sData = ""; }

                                if (iTrack == 1) { sTrack1 = sData; }
                                else if (iTrack == 2) { sTrack2 = sData; }

                                if (sData == "")
                                {
                                    if (sTrack1 == "" && iTrack == 2)//both tracks have no data, remember sTrack2 default == ""
                                    {
                                        if (Display.iCurrentScreen == 1)
                                        {
                                            if (CenCom.bSpanish)
                                            {
                                                Display.ShowMessageBox("Tarjeta insertada incorrectamente.\n\nInsertar su tarjeta otra vez.", 3);
                                            }
                                            else
                                            {
                                                Display.ShowMessageBox("Card inserted incorrectly.\n\nRe-insert card.", 3);
                                            }
                                        }
                                        else if (CenCom.bCardRequest == true)
                                        {
                                            //SetStatus(3);//pd- should be set above
                                            RS485.SendCRS();
                                            if (CenCom.bSpanish)
                                            {
                                                Display.screen0.SetText(Display.screen0.lMsg, "Tarjeta insertada incorrectamente.\n\nInsertar su tarjeta otra vez.");
                                            }
                                            else
                                            {
                                                Display.screen0.SetText(Display.screen0.lMsg, "Card inserted incorrectly.\n\nRe-insert card.");
                                            }
                                            Display.GotoScreen(20, 0);
                                        }
                                        else if (Display.iCurrentScreen == 25)
                                        {
                                            Display.ShowMessageBox("Card inserted incorrectly.\n\nRe-insert card.", 3);
                                        }
                                        else
                                        {
                                            if (CenCom.bSpanish)
                                            {
                                                Display.ShowMessageBox("Su tarjeta no pueda ser leida ahora.", 3);
                                            }
                                            else
                                            {
                                                Display.ShowMessageBox("Can't read card now.", 3);
                                            }
                                        }

                                        if (Display.iCurrentScreen == 25)
                                        {
                                            Display.screen0.SetText(Display.screen0.lPromptTop, "Please swipe card.");
                                        }
                                        //break;
                                    }
                                }
                                else
                                {
                                    bCardRead = true;

                                    iPANCount = 0;
                                    string tempPAN = "";
                                    foreach (char c in sData.Substring(0, sData.Length))
                                    {
                                        if (c >= 48 && c <= 57)
                                        {
                                            iPANCount++;
                                            Debug.WriteLine("iPANCount:" + iPANCount);
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }

                                    //if (sPAN == "")//minumum 13 chars remove last check digit->12chars
                                    //{//pd- commented out because should not matter + allow test to recalculate PAN each time
                                        if (iPANCount >= 13)
                                        {
                                            sPAN = sData.Substring(iPANCount - 13, 12);
                                        }
                                        else if (iPANCount >= 9)
                                        {
                                            sPAN = sData.Substring(0, iPANCount - 1);//remove last check digit
                                            while (sPAN.Length < 12)
                                            {
                                                sPAN = "0" + sPAN;
                                            }
                                        }
                                    //}

                                        //if (sPAN.Substring(0,6) == "007391")//cardlock
                                        //{
                                            //if (sData.Substring(17, 1) == "1")//Driver ID
                                            //{
                                            //    if (sData.Length > 23)
                                            //    {
                                            //        sPAN = sPAN.Substring(6, 6) + sData.Substring(18, 6);
                                            //    }
                                            //}
                                            //else//Vehicle ID
                                            //{
                                            //    if (sData.Length > 29)
                                            //    {
                                            //        sPAN = sPAN.Substring(6, 6) + sData.Substring(24, 6);
                                            //    }
                                            //}
                                        //}
                                }
                            }
                            catch (Exception ex2)
                            {
                                Debug.WriteLine("Track " + iTrack + " data corrupted. " + ex2.Message);
                                if (iTrack == 2)
                                {
                                    Debug.WriteLine("Purge CR SS FOUND BUT COULD NOT READ TRACK DATA: ");//wait for card not read
                                    //PortCR.ReadExisting();//pd- don't purge to prevent wiping out good data;
                                }
                            }
                        }
                        else
                        {
                            if (iTrack == 2)
                            {
                                Debug.WriteLine("Error. Track SS (1 or 2) not found: ");
                                //PortCR.ReadExisting();//pd- don't purge to prevent wiping out good data; WAIT FOR CARD NOT READ
                            }
                        }
                    }
                    catch (Exception ex1)
                    {
                        Debug.WriteLine("Error. Track " + iTrack + ". " + ex1.Message);
                    }
                }

                    if (bCardRead == true)// out of for loop
                    {
                        //Debug.WriteLine("KKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKK");
                        bCardRead = false;

                        if (sTrack1 != "")
                        {
                            sTrack1 = "%" + sTrack1 + "?0";
                        }
                        if (sTrack2 != "")
                        {
                            sTrack2 = ";" + sTrack2 + "?0";
                        }

                        if (CenCom.bCardRequest == true  && bCardLock == false)
                        {
                            CenCom.bCardRequest = false;

                            if (Display.iCurrentScreen == 1)
                            {
                                CenCom.bPreSwipe = true;
                                RS485.PreSwipe();
                            }
                            else
                            {
                                RS485.SendCardData();
                            }
                        }
                        else if (bCardLock == true)//even if bcardrequest == true
                        {
                            Reset();
                            Debug.WriteLine("PURGE CR - DATA READ BUT NO PLACE TO GO");
                            try
                            {
                                sRead = PortCR.ReadExisting();
                                if (CenCom.iLoggingCR == 1)
                                {
                                    WriteLogData_RX(sRead);
                                }
                            }
                            catch
                            {
                            }
                        }
                        else if (Display.iCurrentScreen == 25)
                        {
                            Display.screen0.SetText(Display.screen0.lPromptTop, "PAN= " + sPAN + "\n");
                            Display.screen0.SetText(Display.screen0.lData, "Track 1:" + sTrack1 + "\nTrack 2:" + sTrack2);
                        }
                        else
                        {
                            if (CenCom.bSpanish)
                            {
                                Display.ShowMessageBox("Su tarjeta no pueda ser leida ahora.", 3);
                            }
                            else
                            {
                                Display.ShowMessageBox("Can't read card now.", 3);
                                Debug.WriteLine("Can't read card now.  preswipe:  " + CenCom.bPreSwipe + " bCardRequest- " + CenCom.bCardRequest);
                            }
                            Debug.WriteLine("PURGE CR - DATA READ BUT NO PLACE TO GO");
                            try
                            {
                                sRead = PortCR.ReadExisting();
                                if (CenCom.iLoggingCR == 1)
                                {
                                    WriteLogData_RX(sRead);
                                }
                            }
                            catch
                            {
                            }
                        }

                    }
                    else
                    {

                        Debug.WriteLine("Purge CR TRACK DATA NOT FOUND: ");
                        try
                        {
                            sRead = PortCR.ReadExisting();
                            if (CenCom.iLoggingCR == 1)
                            {
                                WriteLogData_RX(sRead);
                            }
                        }
                        catch
                        {
                        }
                    }
        }

        public static void Reset()
        {
            sPAN = "";
            sTrack1 = "";
            sTrack2 = "";
            sData = "";
        }

        public static void CheckStatus()
        {
            int i;
            bWaitingForResponse = true;
            
            try
            {
                if (PortCR.IsOpen == false)
                {
                    PortCR.Open();
                }
                PortCR.Write(bSend, 0, 2);
                if (CenCom.iLoggingCR == 1)
                {
                    WriteLogData_TX(Environment.NewLine + "TX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                    for (i = 0; i < 2; i++)
                    {
                        WriteLogData_TX(String.Format("{0:X2}", bSend[i]) + " ");
                    }
                }
                Debug.WriteLine("Checking CR status", DateTime.Now.ToString("h:mm:ss.fff"));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public static void SetStatus(int iChangeStatus)
        {
            if (iChangeStatus == 0)
            {
                CardReader.iStatus = iChangeStatus;
            }
            else if (iChangeStatus == 1)
            {
                if (iStatus > 0)
                {
                    CardReader.iStatus = iChangeStatus;
                }
            }
            else if (iChangeStatus == 2)
            {
                if (iStatus > 0)
                {
                    CardReader.iStatus = iChangeStatus;
                }
            }
            else if (iChangeStatus == 3)
            {
                if (iStatus > 0)
                {
                    CardReader.iStatus = iChangeStatus;
                }
            }
        }
        static void WriteLogData_RX(string sData)
        {
            try
            {
                if (CenCom.sOS == "CE")
                {
                    FileAccessCE.AppendFile("\\SDCard\\SPT\\LOGS\\CR_RX_" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
                else
                {
                    File.AppendAllText("LOGS\\CR_RX_" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
            }
            catch
            {

            }
        }

        static void WriteLogData_TX(string sData)
        {
            try
            {
                if (CenCom.sOS == "CE")
                {
                    FileAccessCE.AppendFile("\\SDCard\\SPT\\LOGS\\CR_TX_" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
                else
                {
                    File.AppendAllText("LOGS\\CR_TX_" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
            }
            catch
            {

            }
        }
    }

    public static class CashAcceptor
    {
        public static int iPortNum = 0;
        public static string myData;

        public static SerialPort PortCA = new SerialPort();//pd- made public to for control signal access

        public static bool bWaitingForResponse = false;
        public static int iStatus = 0;
        public static int iEmcastatus = 1;
        static int iPreviousStatus = 0;
        public static string sStatus = "";

        public static int iBytesRead = 0;
        public static int iStatusByte0 = 0;
        public static int iStatusByte1= 0;
        public static int iStatusByte2 = 0;
        public static int iStatusByte3 = 0;

        static int iMsgNum=0;

        public static int iBillVal = 0;
        public static int iBillTotal = 0;

        public static bool bCassetteRemoved = false;

        //PD- rev20
        //public static int iTestMode = 1;

        static DateTime dStartTime;
        static TimeSpan tSpan;

        public static bool bStack = false;
        public static bool bReturn = false;

        public static bool bHold = false;

        public static void Init()
        {
            PortCA.BaudRate = 9600;
            PortCA.StopBits = StopBits.One;
            PortCA.Parity = Parity.Even;
            PortCA.DataBits = 7;
            PortCA.PortName = "COM" + iPortNum;
            PortCA.Handshake = Handshake.None;
            //PortCA.ReceivedBytesThreshold = 1;
            PortCA.ReadTimeout = 1000;//my need to change...

            PortCA.DataReceived += new SerialDataReceivedEventHandler(PortCA_DataReceived);
            if(CenCom.bEmMode == 1)
            {
                iStatus = 1;
                RS485.SendCAS();
            }
            try
            {
                PortCA.Open();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                Debug.WriteLine(ex.Message);
            }
        }

        static void PortCA_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int iBytesToRead = PortCA.BytesToRead;
            byte[] comBuffer = new byte[iBytesToRead];
            int i;

            Debug.WriteLine("CA Data Received");
            Debug.WriteLine("CA bytes to read: " + iBytesToRead);

            if (iStatus < 1) 
            { 
                iStatus = 1;
                Debug.WriteLine("CA Online");
                RS485.SendCAS();
            }
            
            if (bWaitingForResponse == true)
            {
                bWaitingForResponse = false;
                Debug.WriteLine("CA WaitingForResponse Off");
            }

            //if (CenCom.bBillRequest == true)//report status even if not in bill accepting mode (eg cassette removed or bill jam when power up)
            //{

                PortCA.Read(comBuffer, 0, iBytesToRead);
                for (i = 0; i < iBytesToRead; i++)
                {
                    if (iBytesRead == 0 && comBuffer[i] == 2)//checksum could == stx
                    {
                        iBytesRead++;
                        Debug.WriteLine("iBytesRead: " + iBytesRead);
                        Debug.WriteLine("CA- STX");
                    }
                    else if (iBytesRead == 1)
                    {
                        iBytesRead++;
                        Debug.WriteLine("iBytesRead: " + iBytesRead);
                        Debug.WriteLine("CA Length: " + comBuffer[i]);
                    }
                    else if (iBytesRead == 2)
                    {
                        iBytesRead++;
                        Debug.WriteLine("iBytesRead: " + iBytesRead);
                        Debug.WriteLine("MsgNum: " + comBuffer[i]);
                    }
                    else if (iBytesRead == 3)//get status byte 0
                    {
                        iBytesRead++;
                        Debug.WriteLine("iBytesRead: " + iBytesRead);
                        iStatusByte0 = comBuffer[i];
                        Debug.WriteLine("iStatusByte0= " + iStatusByte0);
                    }
                    else if (iBytesRead == 4)//get status byte 1
                    {
                        iBytesRead++;
                        Debug.WriteLine("iBytesRead: " + iBytesRead);
                        iStatusByte1 = comBuffer[i];
                        Debug.WriteLine("iStatusByte1= " + iStatusByte1);
                    }
                    else if (iBytesRead == 5)//get status byte 2
                    {
                        iBytesRead++;
                        Debug.WriteLine("iBytesRead: " + iBytesRead);
                        iStatusByte2 = comBuffer[i];
                        Debug.WriteLine("iStatusByte2= " + iStatusByte2);
                    }
                    //else if (iBytesRead == 6)//get status byte 2
                    //{
                    //    iBytesRead++;
                    //    Debug.WriteLine("iBytesRead: " + iBytesRead);
                    //    iStatusByte3 = comBuffer[i];
                    //    Debug.WriteLine("iStatusByte3= " + iStatusByte3);
                    //}
                    else if (iBytesRead == 6)//get model #
                    {
                        iBytesRead++;
                        Debug.WriteLine("iBytesRead: " + iBytesRead);
                        Debug.WriteLine("model #= " + comBuffer[i]);
                    }
                    else if (iBytesRead == 7)//get revision #
                    {
                        iBytesRead++;
                        Debug.WriteLine("iBytesRead: " + iBytesRead);
                        Debug.WriteLine("revision #= " + comBuffer[i]);
                    }
                    else if (iBytesRead == 8)//get etx
                    {
                        iBytesRead++;
                        Debug.WriteLine("iBytesRead: " + iBytesRead);
                        Debug.WriteLine("etx= " + comBuffer[i]);
                    }
                    else if (iBytesRead == 9)//get bcc
                    {
                        iBytesRead++;
                        Debug.WriteLine("iBytesRead: " + iBytesRead);
                        Debug.WriteLine("bcc= " + comBuffer[i]);
                        Debug.WriteLine("iBytesRead RESET");

                        //if (iStatusByte0 == 4)//PD- Rev 14
                        //{
                        if ((iStatusByte2 & 56) == 8)//PD- 14- read bill early so CAS reports current value
                        {
                            iBillVal = 1;
                            //Display.ShowMessageBox("Adding $1", 2);
                        }
                        else if ((iStatusByte2 & 56) == 16)
                        {
                            iBillVal = 2;
                            //Display.ShowMessageBox("Adding $2", 2);
                        }
                        else if ((iStatusByte2 & 56) == 24)
                        {
                            iBillVal = 5;
                            //Display.ShowMessageBox("Adding $5", 2);
                        }
                        else if ((iStatusByte2 & 56) == 32)
                        {
                            iBillVal = 10;
                            //Display.ShowMessageBox("Adding $10", 2);
                        }
                        else if ((iStatusByte2 & 56) == 40)
                        {
                            iBillVal = 20;
                            //Display.ShowMessageBox("Adding $20", 2);
                        }
                        else if ((iStatusByte2 & 56) == 48)
                        {
                            iBillVal = 50;
                            //Display.ShowMessageBox("Adding $50", 2);
                        }
                        else if ((iStatusByte2 & 56) == 56)
                        {
                            iBillVal = 100;
                            //Display.ShowMessageBox("Adding $100", 2);
                        }
                        else
                        {
                            iBillVal = 0;
                        }

                        Debug.WriteLine("BILL----" + iBillVal);

                        if (Display.iCurrentScreen == 26)
                        {
                            //rev19b3
                            if (iBillVal > 0)
                            {
                                Display.screen0.SetText(Display.screen0.lPromptTop, "Last Bill Value: " + iBillVal);
                            }
                            Return(); //must be for testing screen

                            
                            iBillVal = 0;
                        }

                        iStatus = 1;//PD- rev 15
                        sStatus = "";//PD- rev 15


                        if ((iStatusByte1 & 16) == 0)
                        {
                            bCassetteRemoved = true;
                            CenCom.bBillRequest = false;

                            //iBillVal = 0;PD - rev 16

                            sStatus = "Cassette Removed";
                            iStatus = 13;
                        } //Cassette Removed and other functions
                        else if ((iStatusByte1 & 4) == 4)
                        {
                            sStatus = "Bill Jammed";
                            iStatus = 11;
                        }
                        else if ((iStatusByte1 & 32) == 32)//PD- old MEI - placed here in when debit card inserted after bill is in process of stacking
                        {
                            sStatus = "Paused";
                            iStatus = 15;
                        }
                        else if ((iStatusByte1 & 1) == 1)
                        {
                            sStatus = "Cheated";
                            iStatus = 14;
                        }
                        else if ((iStatusByte1 & 2) == 2)
                        {
                            sStatus = "Rejected";
                            iStatus = 8;
                        }
                        else if ((iStatusByte1 & 8) == 8)
                        {
                            sStatus = "Cassette Full";
                            iStatus = 12;
                        }
                        else if ((iStatusByte2 & 4) == 4)
                        {
                            sStatus = "CA Failure";
                            iStatus = -2;
                        }
                        else if (iStatusByte0 == 32)
                        {
                            sStatus = "Returning";
                            iStatus = 8;
                        }
                        else if ((iStatusByte0 & 64) == 64)
                        {
                            sStatus = "Returned";
                            iStatus = 8;
                        }
                        else if ((iStatusByte0 & 8) == 8)
                        {

                            if (iPreviousStatus == 4)
                            {
                                //PD - rev20 - report stacking of unknown bills
                                if (iBillVal > 0)
                                {
                                    sStatus = "Stacking";
                                    iStatus = 5;
                                }
                                else if (CenCom.iLoggingCA == 1)
                                {
                                    try
                                    {
                                        WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                                        WriteLogData_RX("CA ERROR - STACKING BUT BILL VAL = 0. " + "Status: " + iStatus + ": " + sStatus + " Byte0: " + iStatusByte0 + " Byte1: " + iStatusByte1 + " Byte2: " + iStatusByte2);
                                    }
                                    catch { }
                                }
                            }
                            else if (iPreviousStatus != 5)//PD- so don't repeat
                            {
                                if (CenCom.iLoggingCA == 1)
                                {
                                    try
                                    {
                                        WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                                        WriteLogData_RX("CA ERROR - STACKING BUT PREVIOUS STATE NOT 4,15. " + "Status: " + iStatus + ": " + sStatus + " Byte0: " + iStatusByte0 + " Byte1: " + iStatusByte1 + " Byte2: " + iStatusByte2);
                                    }
                                    catch { }
                                }
                            }
                        }
                        else if ((iStatusByte0 & 16) == 16)
                        {
                            if (iPreviousStatus == 5)
                            {

                                if (iBillVal > 0)
                                {
                                    sStatus = "Stacked";
                                    iStatus = 7;
                                }
                                else if (CenCom.iLoggingCA == 1)
                                {
                                    try
                                    {
                                        WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                                        WriteLogData_RX("CA ERROR - STACKED BUT BILL VAL = 0. " + "Status: " + iStatus + ": " + sStatus + " Byte0: " + iStatusByte0 + " Byte1: " + iStatusByte1 + " Byte2: " + iStatusByte2);
                                    }
                                    catch { }
                                }
                            }
                            else if (iPreviousStatus != 7)//PD - so don't repeat
                            {
                                if (CenCom.iLoggingCA == 1)
                                {
                                    try
                                    {
                                        WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                                        WriteLogData_RX("CA ERROR - STACKED BUT PREVIOUS STATE NOT 5,15,11. " + "Status: " + iStatus + ": " + sStatus + " Byte0: " + iStatusByte0 + " Byte1: " + iStatusByte1 + " Byte2: " + iStatusByte2);
                                    }
                                    catch { }
                                }
                            }
                        }
                        else if ((iStatusByte0 & 4) == 4)
                        {
                            if (iPreviousStatus == 2 || iPreviousStatus == 3 || iPreviousStatus == 7)//PD - rev23 - don't stack right after rejecting
                            {
                                sStatus = "Escrowed";
                                iStatus = 4;
                                dStartTime = DateTime.Now;
                                //Send CAS and NEW Bill
                                try
                                {
                                    //RS485.SendCAS();
                                    if (Display.iCurrentScreen == 7)
                                    {
                                        Display.screen0.SetText(Display.screen0.lPromptTop, "Accepting $" + iBillVal + ". Please Wait...");
                                    }
                                    if (CenCom.bBillRequest)
                                    {
                                        RS485.MySendBill(iBillVal);
                                        RS485.MySendStatus(4, iStatus);
                                    }
                                    
                                }
                                catch 
                                {

                                   
                                }
                            }
                            else if (iPreviousStatus != 4)//PD - so don't repeat
                            {
                                if (CenCom.iLoggingCA == 1)
                                {
                                    try
                                    {
                                        WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                                        WriteLogData_RX("RETURN BILL - ESCROW BUT PREVIOUS STATE NOT 2,3,7. " + "Status: " + iStatus + ": " + sStatus + " Byte0: " + iStatusByte0 + " Byte1: " + iStatusByte1 + " Byte2: " + iStatusByte2);
                                    }
                                    catch { }
                                }
                                //Return();
                            }
                        }
                        else if ((iStatusByte0 & 2) == 2)
                        {
                            sStatus = "Accepting";
                            iStatus = 3;
                        }
                        else if ((iStatusByte0 & 1) == 1 && CenCom.bBillRequest == true)//PD- will set this byte even w/ no bills enabled!!!
                        {
                            sStatus = "Idling";
                            iStatus = 2;
                        }
                        else if ((iStatusByte2 & 1) == 1)//PD- rev 15 - interferes w/ bill jam
                        {
                            sStatus = "CA Power Up";
                            iStatus = -1;
                        }
                        else//PD- rev 15
                        {
                            iStatus = 1;
                            sStatus = "Disabled";
                        }

                        //}
                        
                        iPreviousStatus = iStatus;
                        ChangeStatus();
                        iPreviousStatus = iStatus;

                        iBytesRead = 0;
                        iStatusByte0 = 0;
                        iStatusByte1 = 0;
                        iStatusByte2 = 0;
                        iStatusByte3 = 0;

                        sStatus = "";

                        iBillVal = 0;
                    }
                    else
                    {
                        Debug.WriteLine("CA ERROR BYTE NOT CAUGHT: " + comBuffer[i]);
                        
                        if (CenCom.iLoggingCA == 1)
                        {
                            try
                            {
                                WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                                WriteLogData_RX("CA ERROR BYTE NOT CAUGHT: " + comBuffer[i] + " - Status: " + iStatus + ": " + sStatus + "\nByte0: " + iStatusByte0 + " Byte1: " + iStatusByte1 + " Byte2: " + iStatusByte2);
                            }
                            catch { }
                        }

                        iBytesRead = 0;
                        iStatusByte0 = 0;
                        iStatusByte1 = 0;
                        iStatusByte2 = 0;
                        iStatusByte3 = 0;
                        iBillVal = 0;
                        sStatus = "";
                    }   
                }
            //}
            //else
            //{
            //    Debug.WriteLine("Purge CA: " + PortCA.ReadExisting());
            //}
        }

        static void ChangeStatus()
        {
            if (iStatus != iPreviousStatus)
            {
                if (Display.iCurrentScreen == 26)
                {
                    CenCom.iScreenTimer = 60;//PD- rev20

                    Display.screen0.SetText(Display.screen0.lData, "Status: " + iStatus + ": " + sStatus + "\nByte0: " + iStatusByte0 + " Byte1: " + iStatusByte1 + " Byte2: " + iStatusByte2);
                    
                    //PD- rev19b3
                    Display.screen0.SetText(Display.screen0.lPromptTop, "Bill Value: " + iBillVal);

                    //PD- rev20
                    //if (iStatus == 4 && iTestMode == 1)
                    //{
                    //    Return();
                    //}
                    //else if (iStatus == 4 && iTestMode == 2)
                    //{
                    //    Stack();
                    //}
                    if (iStatus == 4)
                    {
                        Return();
                    }
                }
                else
                {
                    if (CenCom.iLoggingCA == 1)
                    {
                        try
                        {
                            WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                            WriteLogData_RX("Status: " + iStatus + ": " + sStatus + " Byte0: " + iStatusByte0 + " Byte1: " + iStatusByte1 + " Byte2: " + iStatusByte2 + " Previous Status: " + iPreviousStatus);
                        }
                        catch { }
                    }

                    if (iStatus == 2 && iPreviousStatus == 5) //Bill has been accepted and state is returned to Enabled. So we know the bill was accepted properly
                    {
                        iStatus = 7;
                        iBillTotal = iBillTotal + iBillVal;

                        if (CenCom.iLoggingCA == 1)
                        {
                            try
                            {
                                WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                                WriteLogData_RX("Status changed to 7 from 2. " + "Status: " + iStatus + ": " + sStatus + "\nByte0: " + iStatusByte0 + " Byte1: " + iStatusByte1 + " Byte2: " + iStatusByte2);
                            }
                            catch { }
                        }
                    }
                    else if (iStatus == 1 && iPreviousStatus == 15)//PD- status set to 1 and bill stacked (but not reported) after a bill or card is removed from bezel after paused state
                    {
                        iStatus = 7;
                        iBillTotal = iBillTotal + iBillVal;

                        if (CenCom.iLoggingCA == 1)
                        {
                            try
                            {
                                WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                                WriteLogData_RX("Status changed to 7 from 1. " + "Status: " + iStatus + ": " + sStatus + "\nByte0: " + iStatusByte0 + " Byte1: " + iStatusByte1 + " Byte2: " + iStatusByte2);
                            }
                            catch { }
                        }
                    }


                    //else if (iStatus == 4 && iPreviousStatus == 8)//shouldn't need this... caused by lifo rs485 handler
                    //{
                    //    iStatus = 2;
                    //    //don't send if escrow status appears after returning status...
                    //}
                    
                    if (iStatus == 13)//PD- rev22 //Cassette Removed
                    {
                        if (iPreviousStatus == 5 && (iStatusByte0 & 16) == 16)//PD- rev22 //bill stacked properly
                        {
                            sStatus = "Stacked";
                            iStatus = 7;
                            RS485.SendCAS();

                            if (CenCom.iLoggingCA == 1)
                            {
                                try
                                {
                                    WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                                    WriteLogData_RX("STATUS CHANGED FROM CASSETTE REMOVED TO STACKED");
                                }
                                catch { }
                            }
                        }
                        else if (iPreviousStatus == 7)//PD- rev22
                        {
                            iStatus = 1;
                            sStatus = "Disabled";
                            RS485.SendCAS();

                            bHold = true;

                            if (CenCom.iLoggingCA == 1)
                            {
                                try
                                {
                                    WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                                    WriteLogData_RX("STATUS CHANGED FROM CASSETTE REMOVED TO DISABLED");
                                }
                                catch { }
                            }
                        }
                        else if (bHold)//PD- rev22
                        {
                            iStatus = 1;
                            sStatus = "Disabled";

                            if (CenCom.iLoggingCA == 1)
                            {
                                try
                                {
                                    WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                                    WriteLogData_RX("HOLDING DISABLED STATUS");
                                }
                                catch { }
                            }
                        }
                        else 
                        {
                            RS485.SendCAS();
                        }
                    }
                    else if (iStatus == 12)//PD- rev23
                    {
                        if (iPreviousStatus == 5 && (iStatusByte0 & 16) == 16)//PD- rev23
                        {
                            sStatus = "Stacked";
                            iStatus = 7;
                            RS485.SendCAS();

                            if (CenCom.iLoggingCA == 1)
                            {
                                try
                                {
                                    WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                                    WriteLogData_RX("STATUS CHANGED FROM CASSETTE FULL TO STACKED");
                                }
                                catch { }
                            }
                        }
                        else if (iPreviousStatus == 7)//PD- rev23
                        {
                            iStatus = 1;
                            sStatus = "Disabled";
                            RS485.SendCAS();

                            bHold = true;

                            if (CenCom.iLoggingCA == 1)
                            {
                                try
                                {
                                    WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                                    WriteLogData_RX("STATUS CHANGED FROM CASSETTE FULL TO DISABLED");
                                }
                                catch { }
                            }
                        }
                        else if (bHold)//PD- rev23
                        {
                            iStatus = 1;//block from sending cassette removed status with CAE request
                            sStatus = "Disabled";

                            if (CenCom.iLoggingCA == 1)
                            {
                                try
                                {
                                    WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                                    WriteLogData_RX("HOLDING DISABLED STATUS");
                                }
                                catch { }
                            }
                        }
                        else
                        {
                            RS485.SendCAS();
                        }
                    }
                    else
                    {
                        bHold = false;
                    }

                    //PD- rev23 - moved up one level so doesn't trigger on test screen
                    if (iStatus == 11)
                    {
                        if (CenCom.iLoggingCA == 1)
                        {
                            try
                            {
                                WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                                WriteLogData_RX("RETURN CMD SENT AFTER BILL JAM - Status: " + iStatus + ": " + sStatus + " Byte0: " + iStatusByte0 + " Byte1: " + iStatusByte1 + " Byte2: " + iStatusByte2 + " Previous Status: " + iPreviousStatus);
                            }
                            catch { }
                        }
                        Return();
                    }
                    else if (iStatus == 8) //Bill returning
                    {
                        if (CenCom.iLoggingCA == 1)
                        {
                            try
                            {
                                WriteLogData_RX(Environment.NewLine + "RX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                                WriteLogData_RX("RETURN CMD SENT AFTER BILL REJECTED - Status: " + iStatus + ": " + sStatus + " Byte0: " + iStatusByte0 + " Byte1: " + iStatusByte1 + " Byte2: " + iStatusByte2 + " Previous Status: " + iPreviousStatus);
                            }
                            catch { }
                        }
                        Return();
                    }
                } 
            }

            if (iStatus == 4)
            {

                if (bStack == true) 
                {
                    Stack();
                }
                else if (bReturn == true)
                {
                    Return();
                }
            }
            else 
            {
                bStack = false;
                bReturn = false;
            }
        }

        public static void CheckStatus()
        {
            if (CenCom.bEmMode == 1)
            {
                //Send manual status and turn bWaitingForResponse Off
                bWaitingForResponse = false;
                iStatus = iEmcastatus;
            }
            else
            {
                //Debug.WriteLine("CA CHECK STATUS.........................");
                bWaitingForResponse = true;

                byte[] bSend = new byte[7];
                byte bcc = 0;
                int i;

                bSend[0] = 2;
                bSend[1] = 7;
                bSend[2] = (byte)(16 + iMsgNum);
                iMsgNum = NextMsg();
                bSend[3] = 0;
                bSend[4] = 0;
                //bSend[5] = 0;
                bSend[5] = 3;

                for (i = 1; i < 5; i++)
                {
                    bcc = (byte)(bcc ^ bSend[i]);
                    //Debug.WriteLine(i);
                    //Debug.WriteLine(bcc);
                }
                bSend[6] = bcc;

                try
                {
                    if (PortCA.IsOpen == false)
                    {
                        PortCA.Open();
                    }
                    PortCA.Write(bSend, 0, 7);

                    Debug.WriteLine("Checking CA status", DateTime.Now.ToString("h:mm:ss.fff"));
                    for (i = 0; i < 7; i++)
                    {
                        Debug.Write(bSend[i] + "-");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        public static void Enable()
        {
            byte[] bSend = new byte[7];
            byte bcc = 0;
            int i;

            

            bSend[0] = 2;
            bSend[1] = 7;
            bSend[2] = (byte)(16 + iMsgNum);
            iMsgNum = NextMsg();
            bSend[3] = 127;
            bSend[4] = 28;
            //bSend[5] = 0;
            bSend[5] = 3;

            for (i = 1; i < 5; i++)
            {
                bcc = (byte)(bcc ^ bSend[i]);
            }
            bSend[6] = bcc;

            try
            {
                if (PortCA.IsOpen == false)
                {
                    PortCA.Open();
                }
                PortCA.Write(bSend, 0, 7);

                Debug.WriteLine("ENABLE CA");
                for (i = 0; i < 7; i++)
                {
                    Debug.Write(bSend[i] + "-");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public static void Stack()
        {
            byte[] bSend = new byte[7];
            byte bcc = 0;
            int i;

            bStack = true;

            bSend[0] = 2;
            bSend[1] = 7;
            bSend[2] = (byte)(16 + iMsgNum);
            iMsgNum = NextMsg();
            bSend[3] = 127;
            bSend[4] = 60;
            //bSend[5] = 0;
            bSend[5] = 3;

            for (i = 1; i < 5; i++)
            {
                bcc = (byte)(bcc ^ bSend[i]);
            }
            bSend[6] = bcc;

            try
            {
                if (PortCA.IsOpen == false)
                {
                    PortCA.Open();
                }
                PortCA.Write(bSend, 0, 7);
                if (CenCom.iLoggingCA == 1)
                {
                    WriteLogData_TX(Environment.NewLine + "TX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                    for (i = 0; i < 7; i++)
                    {
                        WriteLogData_TX(String.Format("{0:X2}", bSend[i]) + " ");
                        Debug.Write(bSend[i] + "-");
                    }
                }
                Debug.WriteLine("STACK BILL");
                for (i = 0; i < 7; i++)
                {
                    Debug.Write(bSend[i] + "-");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public static void Return()
        {
            byte[] bSend = new byte[7];
            byte bcc = 0;
            int i;

            bStack = false;

            bSend[0] = 2;
            bSend[1] = 7;
            bSend[2] = (byte)(16 + iMsgNum);
            iMsgNum = NextMsg();
            bSend[3] = 127;
            bSend[4] = 92;
            //bSend[5] = 0;
            bSend[5] = 3;

            for (i = 1; i < 5; i++)
            {
                bcc = (byte)(bcc ^ bSend[i]);
            }
            bSend[6] = bcc;

            try
            {
                if (PortCA.IsOpen == false)
                {
                    PortCA.Open();
                }
                PortCA.Write(bSend, 0, 7);
                if (CenCom.iLoggingCA == 1)
                {
                    WriteLogData_TX(Environment.NewLine + "TX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                    for (i = 0; i < 7; i++)
                    {
                        WriteLogData_TX(String.Format("{0:X2}", bSend[i]) + " ");
                        Debug.Write(bSend[i] + "-");
                    }
                }
                Debug.WriteLine("RETURN BILL");
                for (i = 0; i < 7; i++)
                {
                    Debug.Write(bSend[i] + "-");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public static void Disable()
        {
            byte[] bSend = new byte[7];
            byte bcc = 0;
            int i;

            bSend[0] = 2;
            bSend[1] = 7;
            bSend[2] = (byte)(16 + iMsgNum);
            iMsgNum = NextMsg();
            bSend[3] = 0;
            //bSend[4] = 0;
            bSend[4] = 16;//CA PROBLEM IN OREGON - CA STACKS BILL IF DISABLED WHILE ACCEPTING
            //bSend[5] = 0;
            bSend[5] = 3;

            for (i = 1; i < 5; i++)
            {
                bcc = (byte)(bcc ^ bSend[i]);
            }
            bSend[6] = bcc;

            try
            {
                if (PortCA.IsOpen == false)
                {
                    PortCA.Open();
                }
                PortCA.Write(bSend, 0, 7);

                Debug.WriteLine("DISABLE CA");
                for (i = 0; i < 7; i++)
                {
                    Debug.Write(bSend[i] + "-");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        static int NextMsg()
        {
            if (iMsgNum == 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        static void WriteLogData_RX(string sData)
        {
            try
            {
                if (CenCom.sOS == "CE")
                {
                    FileAccessCE.AppendFile("\\SDCard\\SPT\\LOGS\\CA_STATUS_" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
                else
                {
                    File.AppendAllText("LOGS\\CA_STATUS_" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
            }
            catch
            {

            }
        }

        static void WriteLogData_TX(string sData)
        {
            try
            {
                if (CenCom.sOS == "CE")
                {
                    FileAccessCE.AppendFile("\\SDCard\\SPT\\LOGS\\CA_TX" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
                else
                {
                    File.AppendAllText("LOGS\\CA_TX" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
            }
            catch
            {

            }
        }
    }


    public static class Printer
    {
        public static int iPortNum = 0;

        public static SerialPort PortPTR = new SerialPort();//CE pd- made public to for control signal access

        public static bool bWaitingForResponse = false;
        public static int iStatus = 0;

        public static string sReceipt;

        public static bool bPrinting = false;

        static int iPrintCheckCount = 0;

        public static int iType = 1;

        static string sRead = "";

        public static bool bError = false;
        public static string sStatus = "";

        //public static ManagementObjectSearcher searcher;

        public static void Init()
        {
            if (iType == 4)
            {
                PortPTR.BaudRate = 9600;
                PortPTR.StopBits = StopBits.One;
                PortPTR.Parity = Parity.None;

                PortPTR.DataBits = 8;
                PortPTR.PortName = "COM" + iPortNum;
                PortPTR.Handshake = Handshake.None;

                CenCom.iPrinterWait = 1;
            } 
            else if (iType == 3)
            {
            //    ManagementScope MyScope = new ManagementScope();
            //    ManagementScope scope = new ManagementScope(@"\root\cimv2");
            //    scope.Connect();

            //    searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer");

            //    PrinterUSB.OpenPrinter("MyPrinter");
            //    CenCom.iPrinterWait = 1;
            }
            else if (iType == 2)
            {
                PortPTR.BaudRate = 115200;
                PortPTR.StopBits = StopBits.One;
                PortPTR.Parity = Parity.None;
                PortPTR.DataBits = 8;
                PortPTR.PortName = "COM" + iPortNum;
                PortPTR.Handshake = Handshake.RequestToSend;

                CenCom.iPrinterWait = 1;
            }
            else
            {
                PortPTR.BaudRate = 1200;
                PortPTR.StopBits = StopBits.One;
                PortPTR.Parity = Parity.None;
                PortPTR.DataBits = 8;
                PortPTR.PortName = "COM" + iPortNum;
                PortPTR.Handshake = Handshake.None;

                CenCom.iPrinterWait = 6;
            }
            
            //PortPTR.ReceivedBytesThreshold = 1;
            PortPTR.ReadTimeout = 1000;//may need to change...

            PortPTR.DataReceived += new SerialDataReceivedEventHandler(PortPTR_DataReceived);

            try
            {
                PortPTR.Open();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                Debug.WriteLine(ex.Message);
            }
        }

        static void PortPTR_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int iBytesToRead = PortPTR.BytesToRead;
            int i;
            byte[] comBuffer = new byte[iBytesToRead];
            byte bCurrentByte;

            Debug.WriteLine("Printer Data Received");
            Debug.WriteLine("Printer bytes to read: " + iBytesToRead);

            if (iType == 2)
            {
                if (iBytesToRead > 3)
                {
                    try
                    {
                        PortPTR.Read(comBuffer, 0, iBytesToRead);
                    }
                    catch
                    {
                    }

                    bCurrentByte = 0;
                    for (i = 0; i < iBytesToRead; i++)
                    {
                        bCurrentByte = comBuffer[i];
                        Debug.WriteLine("PRINTER JAM?????-" + String.Format("{0:X2}", bCurrentByte));
                        if (bCurrentByte == 0x80) 
                        {
                            break;
                        }
                    }

                    if ((bCurrentByte & 0x80) != 0x80)
                    {
                        Printer.bError = true;
                        Printer.sStatus = "Printer Error";
                        Debug.WriteLine("Printer Error");
                    }
                    else if ((bCurrentByte & 0x04) == 0x04)
                    {
                        Printer.bError = true;
                        Printer.sStatus = "Printer Temp";
                        Debug.WriteLine("Printer Temp");
                    }
                    else if ((bCurrentByte & 0x40) == 0x40)
                    {
                        Printer.bError = true;
                        Printer.sStatus = "Paper Out";
                        Debug.WriteLine("Paper Out");
                    }
                    else if ((bCurrentByte & 0x10) == 0x10)
                    {
                        Printer.bError = true;
                        Printer.sStatus = "Paper Jam";
                        Debug.WriteLine("Paper Jam");
                    }
                    else if ((bCurrentByte & 0x20) == 0x20)
                    {
                        Printer.bError = false;
                        Printer.sStatus = "Paper Low";
                        Debug.WriteLine("Paper Low");
                    }
                    else if ((bCurrentByte & 0x08) == 0x08)
                    {
                        Printer.bError = false;
                        Printer.sStatus = "Printout Lost";
                        Debug.WriteLine("Printout Lost");
                    }
                    else if ((bCurrentByte & 0x02) == 0x02)
                    {
                        Printer.bError = false;
                        Printer.sStatus = "Paper In Chute";
                        Debug.WriteLine("Paper In Chute");
                    }
                    else  //if ((ptrCharReadArray[0] & 0x80) == 0x80)
                    {
                        Printer.bError = false;
                        Printer.sStatus = "";
                        Debug.WriteLine("Printer OK");
                    }

                    //if (bCurrentByte == 0x80)
                    if (bError == false)
                    {
                        if (iStatus < 1)//distinguish offline from jam - only trigger if offline
                        {
                            iStatus = 1;
                            //bError = false;
                            Debug.WriteLine("Printer Online");
                            RS485.SendPRS();
                            Config();
                        }
                    }
                    else
                    {
                        if (iStatus > 0)
                        {
                            iStatus = 0;
                            //bError = true;
                            Debug.WriteLine("Printer Jam");
                            RS485.SendPRS();
                        }
                        //else if (bError == false)//if back online with paper jam status - still shouldn't print but change error msg
                        //{
                        //    bError = true;
                        //    Debug.WriteLine("Printer Jam");
                        //}
                    }
                }
            }
            else
            {
                if (iStatus < 1)
                {
                    iStatus = 1;
                    Debug.WriteLine("Printer Online");
                    RS485.SendPRS();
                    Config();
                }

                Debug.WriteLine("Purge Printer: ");
                try
                {
                    sRead = PortPTR.ReadExisting();
                    if (CenCom.iLoggingPR == 1)
                    {
                        WriteLogData_RX(sRead);//would fill up log
                        for (i = 0; i < sRead.Length; i++)//PD- caused H3.exe failure
                        {
                            WriteLogData_RX(String.Format("{0:X2}", (byte)Convert.ToChar(sRead.Substring(i, 1))) + " ");
                        }
                    }
                }
                catch
                {
                }
            }

            if (bWaitingForResponse == true)
            {
                bWaitingForResponse = false;
                Debug.WriteLine("Printer WaitingForResponse Off");
            }
        }

        public static void CheckStatus()
        {
            int i;
            byte[] bSend = new byte[3];

            if (iType == 4)
            {
                bSend = new byte[] { 0x1B, 0x76 };
            }
            else
            {
                bSend = new byte[] { 28, 114, 255 };
            }

            bWaitingForResponse = true;

            try
            {
                if (PortPTR.IsOpen == false)
                {
                    PortPTR.Open();
                }
                if (iType == 4)
                {
                    PortPTR.Write(bSend, 0, 2);
                }
                else
                {
                    PortPTR.Write(bSend, 0, 3);
                }
                if (CenCom.iLoggingPR == 1)
                {
                    WriteLogData_TX(Environment.NewLine + "TX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                    for (i = 0; i < 3; i++)
                    {
                        WriteLogData_TX(String.Format("{0:X2}", bSend[i]) + " ");
                    }
                }
                Debug.WriteLine("Checking Printer status", DateTime.Now.ToString("h:mm:ss.fff"));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public static void Config()
        {
            int i;
            byte[] bSend = new byte[9];

            if (iType == 4)
            {
                bSend = new byte[] { 0x1B, 0x21, 0x00, 0x1D, 0x4C, 0x30, 0x00 };
            }
            else
            {
                bSend = new byte[] { 28, 57, 95, 27, 33, 34, 29, 97, 22 };
            }

            try
            {
                if (PortPTR.IsOpen == false)
                {
                    PortPTR.Open();
                }

                if (iType == 4)
                {
                    PortPTR.Write(bSend, 0, 7);
                }
                else
                {
                    PortPTR.Write(bSend, 0, 9);
                }

                if (CenCom.iLoggingPR == 1)
                {
                    WriteLogData_TX(Environment.NewLine + "TX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                    for (i = 0; i < 9; i++)
                    {
                        WriteLogData_TX(String.Format("{0:X2}", bSend[i]) + " ");
                    }
                }
                Debug.WriteLine("Configuring Printer.................");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public static bool Test()
        {
            //byte[] bSend = new byte[] { 10,10,10,10,10,10,10,10,10,10,48,49,50,51,52,53,54,55,56,57,10,10,10,10,10,10,10,10,10,10,29,86,48};
            byte[] bSend = new byte[1];
            byte[] bSend10 = new byte[] {10};
            byte[] bSendCut = new byte[] { 29, 86, 48 };

            int i,j,k = 0;

            bPrinting = true;
            iPrintCheckCount = 0;
            CenCom.iPrintTimer = 0;

            //CenCom.BeepOff();

            try
            {
                if (PortPTR.IsOpen == false)
                {
                    PortPTR.Open();
                }

                k = 15;
                for (i = 0; i < 30; i++)
                {
                    for (j = 0; j < 27; j++)
                    {
                        bSend[0] = (byte)(k + 33);
                        PortPTR.Write(bSend, 0, 1);
                        if (k < 93) { k++; }
                        else { k = 0; }
                    }
                    PortPTR.Write(bSend10, 0, 1);
                }

                for (i = 0; i <= 8; i++)
                {
                    PortPTR.Write(bSend10, 0, 1);
                }

                if (iType == 1)
                {
                    PortPTR.Write(bSendCut, 0, 3);
                }
                Debug.WriteLine("Testing Printer");

                //CheckStatus();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        static void WriteLogData_RX(string sData)
        {
            try
            {
                if (CenCom.sOS == "CE")
                {
                    FileAccessCE.AppendFile("\\SDCard\\SPT\\LOGS\\PR_RX" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
                else
                {
                    File.AppendAllText("LOGS\\PR_RX" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
            }
            catch
            {

            }
        }

        public static bool Print(string sPrint)
        {
            byte[] bSend = new byte[1];
            byte[] bSendCut = new byte[] { 29, 86, 48 };

            int i = 0;

            bPrinting = true;
            iPrintCheckCount = 0;
            CenCom.iPrintTimer = 0;

            //CenCom.BeepOff();

            try
            {
                if (PortPTR.IsOpen == false)
                {
                    PortPTR.Open();
                }
                if (CenCom.iLoggingPR == 1)
                {
                    WriteLogData_TX(Environment.NewLine + "TX> " + DateTime.Now.ToString("HH:mm:ss:ffff") + " -- ");
                }
                for (i = 0; i < sPrint.Length; i++)
                {
                    bSend[0] = (byte)Convert.ToChar(sPrint.Substring(i,1));
                    PortPTR.Write(bSend, 0, 1);
                    if (CenCom.iLoggingPR == 1)
                    {
                        WriteLogData_TX(sPrint.Substring(i, 1));
                    }
                }

                if (iType == 1)
                {
                    PortPTR.Write(bSendCut, 0, 3);
                }
                if (CenCom.iLoggingPR == 1)
                {
                    for (i = 0; i < 3; i++)
                    {
                        WriteLogData_TX(String.Format("{0:X2}", bSendCut[i]) + " ");
                    }
                }
                
                Debug.WriteLine("Printed Receipt");

                //CheckStatus();

                return true;
            }
            catch (Exception ex)
            {
                //iStatus = 1;
                //RS485.SendPRS();
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        static void WriteLogData_TX(string sData)
        {
            try
            {
                if (CenCom.sOS == "CE")
                {
                    FileAccessCE.AppendFile("\\SDCard\\SPT\\LOGS\\PR_TX_" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
                else
                {
                    File.AppendAllText("LOGS\\PR_TX_" + String.Format("{0:D2}", DateTime.Today.Day) + ".txt", sData);
                }
            }
            catch
            {

            }
        }
    }

    public static class FileAccess
    {
        public static string sSettings;
        public static string sVersion;

        public static string GetTestPAN()
        {
            return "401234567890";
            //return "095000000000";
        }
    }

    public static class FileAccessCE
    {
        public static string sSettings;
        public static string sVersion;

        private static Int32 METHOD_BUFFERED = 0;
        private static Int32 FILE_ANY_ACCESS = 0;
        private static Int32 FILE_DEVICE_HAL = 0x00000101;

        private const Int32 ERROR_NOT_SUPPORTED = 0x32;
        private const Int32 ERROR_INSUFFICIENT_BUFFER = 0x7A;

        private static Int32 IOCTL_HAL_GET_DEVICEID =
            ((FILE_DEVICE_HAL) << 16) | ((FILE_ANY_ACCESS) << 14)
            | ((21) << 2) | (METHOD_BUFFERED);

        private static Int32 IOCTL_HAL_GET_UBOOTID =
            ((FILE_DEVICE_HAL) << 16) | ((FILE_ANY_ACCESS) << 14)
            | ((4017) << 2) | (METHOD_BUFFERED);

        [DllImport("coredll.dll", SetLastError = true)]
        private static extern bool KernelIoControl(Int32 dwIoControlCode,
            IntPtr lpInBuf, Int32 nInBufSize, byte[] lpOutBuf,
            Int32 nOutBufSize, ref Int32 lpBytesReturned);

        private static string GetDeviceID()
        {
            byte[] outbuff = new byte[100];
            Int32 dwOutBytes;
            bool done = false;

            Int32 nBuffSize = outbuff.Length;

            BitConverter.GetBytes(nBuffSize).CopyTo(outbuff, 0);
            dwOutBytes = 0;

            while (!done)
            {
                if (KernelIoControl(IOCTL_HAL_GET_DEVICEID, IntPtr.Zero,
                    0, outbuff, nBuffSize, ref dwOutBytes))
                {
                    done = true;
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();

                    switch (error)
                    {
                        case ERROR_NOT_SUPPORTED:
                            throw new NotSupportedException(
                                "IOCTL_HAL_GET_DEVICEID is not supported on this device",
                                new Win32Exception(error));

                        case ERROR_INSUFFICIENT_BUFFER:

                            nBuffSize = BitConverter.ToInt32(outbuff, 0);
                            outbuff = new byte[nBuffSize];

                            BitConverter.GetBytes(nBuffSize).CopyTo(outbuff, 0);
                            break;

                        default:
                            throw new Win32Exception(error, "Unexpected error");
                    }
                }
            }

            Int32 dwPresetIDOffset = BitConverter.ToInt32(outbuff, 0x4);
            Int32 dwPresetIDSize = BitConverter.ToInt32(outbuff, 0x8);
            Int32 dwPlatformIDOffset = BitConverter.ToInt32(outbuff, 0xc);
            Int32 dwPlatformIDSize = BitConverter.ToInt32(outbuff, 0x10);
            StringBuilder sb = new StringBuilder();

            for (int i = dwPresetIDOffset;
                i < dwPresetIDOffset + dwPresetIDSize; i++)
            {
                sb.Append(String.Format("{0:X2}", outbuff[i]));
            }

            sb.Append("-");

            for (int i = dwPlatformIDOffset;
                i < dwPlatformIDOffset + dwPlatformIDSize; i++)
            {
                sb.Append(String.Format("{0:X2}", outbuff[i]));
            }
            return sb.ToString();
        }

        public static bool CheckUBootID()
        {
            byte[] outbuff = new byte[128];
            Int32 dwOutBytes;
            bool done = false;

            Int32 nBuffSize = outbuff.Length;

            BitConverter.GetBytes(nBuffSize).CopyTo(outbuff, 0);
            dwOutBytes = 0;

            while (!done)
            {
                if (KernelIoControl(IOCTL_HAL_GET_UBOOTID, IntPtr.Zero,
                    0, outbuff, nBuffSize, ref dwOutBytes))
                {
                    done = true;
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();

                    switch (error)
                    {
                        case ERROR_NOT_SUPPORTED:
                            throw new NotSupportedException(
                                "IOCTL_HAL_GET_DEVICEID is not supported on this device",
                                new Win32Exception(error));

                        case ERROR_INSUFFICIENT_BUFFER:

                            nBuffSize = BitConverter.ToInt32(outbuff, 0);
                            outbuff = new byte[nBuffSize];

                            BitConverter.GetBytes(nBuffSize).CopyTo(outbuff, 0);
                            break;

                        default:
                            throw new Win32Exception(error, "Unexpected error");
                    }
                }
            }

            StringBuilder sb = new StringBuilder();
            string myStr = "";

            for (int i = 0;
                i < 128; i++)
            {
                Debug.WriteLine(i + " " + Convert.ToChar(outbuff[i]));
                //sb.Append(String.Format("{0:X2}", outbuff[i]));
                if (outbuff[i] > 20)
                {
                    sb.Append(Convert.ToChar(outbuff[i]));
                    myStr = myStr + Convert.ToChar(outbuff[i]);
                }

            }

            Debug.WriteLine("SB=" + sb);
            Debug.WriteLine("myStr=" + myStr);
            if (myStr.Contains("00:19:b8"))
            {
                Debug.WriteLine("ThEre it IS!!!");

                return true;
            }
            else
            {
                Debug.WriteLine("ThEre it IS NOT!!!");

                return true;
            }
            //return sb.ToString();
        }

        public static bool Check()
        {
            try
            {
                string strDeviceID = GetDeviceID();
                Debug.WriteLine("Device ID: " + strDeviceID);

                RegistryKey key = Registry.LocalMachine.OpenSubKey("Comm\\NDIS\\Parms");
                if (key != null)
                {
                    if ((string)key.GetValue("Base") == Crc.ComputeChecksum(strDeviceID))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                //Display.ShowMessageBox("Device Error: " + ex.Message.ToString(), 3);
                Display.ShowMessageBox("Device Error", 3);
                return false;
            }
        }

        public static void AppendFile(string sPath, string sText)
        {
            byte[] buffer;
            FileStream fileStream = new FileStream(sPath, FileMode.Append, System.IO.FileAccess.Write);

            try
            {
                int length = sText.Length;
                buffer = new byte[length];
                int count;

                for (count = 0; count < length; count++)
                {
                    fileStream.WriteByte(Convert.ToByte(sText[count]));
                }
            }
            finally
            {
                fileStream.Close();
            }
        }

        public static void WriteFile(string sPath, string sText)
        {
            byte[] buffer;
            FileStream fileStream = new FileStream(sPath, FileMode.Open, System.IO.FileAccess.Write);

            try
            {
                //int length = (int)fileStream.Length;
                int length = sText.Length;
                buffer = new byte[length];
                int count;

                for (count = 0; count < length; count++)
                {
                    fileStream.WriteByte(Convert.ToByte(sText[count]));
                }
            }
            finally
            {
                fileStream.Close();
            }
        }

        public static byte[] ReadFile(string sPath)
        {
            byte[] buffer;
            FileStream fileStream = new FileStream(sPath, FileMode.Open, System.IO.FileAccess.Read);

            try
            {
                int length = (int)fileStream.Length;
                buffer = new byte[length];
                int count;
                int sum = 0;
                int i;

                while ((count = fileStream.Read(buffer, sum, length - sum)) > 0)
                {
                    sum += count;
                }

                for (count = 0; count < length; count++)
                {
                    sSettings = sSettings + Convert.ToChar(buffer[count]);
                }
                FileAccess.sSettings = sSettings;
            }
            finally
            {
                fileStream.Close();
            }
            return buffer;
        }

        public static void ParseSettings()
        {
            Debug.WriteLine(FileAccess.sSettings);

            RS485.iPortNum = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<RS_PORT>") + 9, 1));
            EPP.iPortNum = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<KP_PORT>") + 9, 2));
            CashAcceptor.iPortNum = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<CA_PORT>") + 9, 1));
            CardReader.iPortNum = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<CR_PORT>") + 9, 1));
            Printer.iPortNum = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<PR_PORT>") + 9, 2));
            EPP.iEncryptionMethod = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<ENC_METHOD>") + 12, 1));
            CenCom.iPICID = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<PICID>") + 7, 1));
            CenCom.sPICID = Convert.ToString(CenCom.iPICID);
            sVersion = sSettings.Substring(sSettings.IndexOf("<VERSION>") + 9, 9);
            Printer.iType = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<PTR_TYPE>") + 10, 1));
            Display.iType = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<LCD_TYPE>") + 10, 1));
            //Fujitsu
            EPP.iType = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<EPP_TYPE>") + 10, 1));
            CenCom.iBrand = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<BRAND>") + 7, 1));
            //CenCom.sOS = sSettings.Substring(sSettings.IndexOf("<OS>") + 4, 2);

            if (CenCom.bLogMaster)
            {
                CenCom.iLoggingSSC = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<LOG_SSC>") + 9, 1));
                CenCom.iLoggingEPP = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<LOG_EPP>") + 9, 1));
                CenCom.iLoggingCA = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<LOG_CA>") + 8, 1));
                CenCom.iLoggingCR = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<LOG_CR>") + 8, 1));
                CenCom.iLoggingPR = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<LOG_PR>") + 8, 1));
            }

            FileAccess.sVersion = sVersion;
        }

        public static string GetTestPAN()
        {
            return "401234567890";
        }

        public static bool CopyLogs()
        {
            DateTime dt = DateTime.Now;
            string sFileName = "";
            string sDestFile = "";
            //string sSourcePath = @"C:\Documents and Settings\Administrator\My Documents\H3\LOGS";
            //string sTargetPath = @"D:\LOGS\" + "PIC" + CenCom.sPICID + "_" + dt.Year + "_" + dt.Month + "_" + dt.Day + "_" + dt.Hour + "_" + dt.Minute + "_" + dt.Second;
            string sSourcePath = "\\SDCard\\SPT\\LOGS\\";
            string sTargetPath = "\\HardDisk\\LOGS\\" + "SPT_" + CenCom.sPICID + "_" + dt.Year + "_" + dt.Month + "_" + dt.Day + "_" + dt.Hour + "_" + dt.Minute + "_" + dt.Second;

            if (System.IO.Directory.Exists("\\HardDisk\\LOGS"))
            {
                Debug.WriteLine("MMMMMMMMMMMM");
                if (!System.IO.Directory.Exists(sTargetPath))
                {
                    Debug.WriteLine("NNNNNNNNNNNNNNN");
                    Debug.WriteLine(sTargetPath);
                    System.IO.Directory.CreateDirectory(sTargetPath);
                }
                if (System.IO.Directory.Exists(sSourcePath))
                {
                    string[] files = System.IO.Directory.GetFiles(sSourcePath);

                    foreach (string s in files)
                    {
                        sFileName = System.IO.Path.GetFileName(s);
                        sDestFile = System.IO.Path.Combine(sTargetPath, sFileName);
                        System.IO.File.Copy(s, sDestFile, true);
                        System.IO.File.Delete(s);
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }//FILEACCESS_CE

    public static class FileAccessXP
    {
        public static string sSettings;
        public static string sVersion;

        public static void ReadSettings()
        {
            sSettings = File.ReadAllText("settings.txt");
            FileAccess.sSettings = sSettings;

            RS485.iPortNum = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<RS_PORT>") + 9, 1));
            EPP.iPortNum = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<KP_PORT>") + 9, 2));
            CashAcceptor.iPortNum = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<CA_PORT>") + 9, 1));
            CenCom.bEmMode = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<EM_MODE>") + 9, 1));
            CenCom.inputType = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<INPUT_TYPE>") + 12, 1));
            CardReader.iPortNum = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<CR_PORT>") + 9, 1));
            Printer.iPortNum = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<PR_PORT>") + 9, 1));
            EPP.iEncryptionMethod = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<ENC_METHOD>") + 12, 1));
            CenCom.iPICID = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<PICID>") + 7, 1));
            CenCom.sPICID = Convert.ToString(CenCom.iPICID);
            sVersion = sSettings.Substring(sSettings.IndexOf("<VERSION>") + 9, 9);
            Printer.iType = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<PTR_TYPE>") + 10, 1));
            Display.iType = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<LCD_TYPE>") + 10, 1));
            //Fujitsu
            EPP.iType = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<EPP_TYPE>") + 10, 1));
            CenCom.iBrand = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<BRAND>") + 7, 1));

            if (CenCom.bLogMaster)
            {
                CenCom.iLoggingSSC = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<LOG_SSC>") + 9, 1));
                CenCom.iLoggingEPP = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<LOG_EPP>") + 9, 1));
                CenCom.iLoggingCA = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<LOG_CA>") + 8, 1));
                CenCom.iLoggingCR = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<LOG_CR>") + 8, 1));
                CenCom.iLoggingPR = Convert.ToInt16(sSettings.Substring(sSettings.IndexOf("<LOG_PR>") + 8, 1));
            }

            FileAccess.sVersion = sVersion;
        }

        public static void GetEncryptMode()
        {
            //EPP.iEncryptionMethod = 0;
            //EPP.iEncryptionMethod = Convert.ToInt16(Convert.ToString(charData[5]));

        }

        public static void GetPICID()
        {
            //CenCom.iPICID = Convert.ToInt16(CenCom.sPICID);
            //CenCom.iPICID = Convert.ToInt16(Convert.ToString(charData[6]));
            //CenCom.sPICID = Convert.ToString(CenCom.iPICID);
        }

        public static string GetTestPAN()
        {
            return "401234567890";
            //return "095000000000";
        }

        public static void CopyLogs()
        {
            DateTime dt = DateTime.Now;
            string sFileName = "";
            string sDestFile = "";
            string sSourcePath = @"C:\Documents and Settings\Administrator\My Documents\H3\LOGS";
            string sTargetPath = @"D:\LOGS\" + "PIC" + CenCom.sPICID + "_" + dt.Year + "_" + dt.Month + "_" + dt.Day + "_" + dt.Hour + "_" + dt.Minute + "_" + dt.Second;

            if (!System.IO.Directory.Exists(sTargetPath))
            {
                Debug.WriteLine(sTargetPath);
                System.IO.Directory.CreateDirectory(sTargetPath);
            }

            if (System.IO.Directory.Exists(sSourcePath))
            {
                string[] files = System.IO.Directory.GetFiles(sSourcePath);

                foreach (string s in files)
                {
                    sFileName = System.IO.Path.GetFileName(s);
                    sDestFile = System.IO.Path.Combine(sTargetPath, sFileName);
                    System.IO.File.Copy(s, sDestFile, true);
                    System.IO.File.Delete(s);
                }
            }
        }

        //public static void DeleteLogs()
        //{
        //    string sSourcePath = @"C:\Documents and Settings\Bus1\My Documents\LOGS";

        //    if (System.IO.Directory.Exists(sSourcePath))
        //    {
        //        string[] files = System.IO.Directory.GetFiles(sSourcePath);

        //        foreach (string s in files)
        //        {
        //            System.IO.File.Delete(s);
        //        }
        //    }
        //}
    }//FILEACCESS_XP

    public static class Crc
    {
        const ushort polynomial = 0x0001;
        static ushort[] table = new ushort[256];

        public static void InitTable()
        {
            ushort value;
            ushort temp;
            for (ushort i = 0; i < table.Length; ++i)
            {
                value = 0;
                temp = i;
                for (byte j = 0; j < 8; ++j)
                {
                    if (((value ^ temp) & 0x0001) != 0)
                    {
                        value = (ushort)((value >> 1) ^ polynomial);
                    }
                    else
                    {
                        value >>= 1;
                    }
                    temp >>= 1;
                }
                table[i] = value;
            }
        }

        public static string ComputeChecksum(string sBytes)
        {
            ushort crc = 0;
            int iCheckVal1, iCheckVal2;

            for (int i = 0; i < sBytes.Length; ++i)
            {
                byte index = (byte)(crc ^ (char)sBytes[i]);
                crc = (ushort)((crc >> 8) ^ table[index]);
            }

            iCheckVal1 = (byte)(crc >> 8);
            iCheckVal2 = (byte)(crc & 0x00ff);

            return String.Format("{0:X2}", (iCheckVal1)) + String.Format("{0:X2}", (iCheckVal2));
        }
        
    }//CRC
}
