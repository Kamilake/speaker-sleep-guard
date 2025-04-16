// MIT License
// Copyright (c) 2025 Kamilake
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// dotnet build && dotnet run
// dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o ./publish
using System;
using System.Windows.Forms;
using Microsoft.Win32;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Diagnostics;
using System.Management;
using System.IO;
using System.Reflection;
using System.Drawing;

namespace SpeakerSleepGuard
{
    /// <summary>
    /// 애플리케이션의 상수와 설정을 정의합니다.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// 애플리케이션 이름입니다.
        /// </summary>
        public const string ApplicationName = "Speaker Sleep Guard";

        /// <summary>
        /// 애플리케이션 표시 이름입니다.
        /// </summary>
        public const string DisplayName = "스피커 활성 유지";

        /// <summary>
        /// 시스템 부팅 후 자동 실행 간주 시간(분)입니다.
        /// </summary>
        public const int SystemStartupThresholdMinutes = 5;

        /// <summary>
        /// 오디오 샘플링 레이트(Hz)입니다.
        /// </summary>
        public const int AudioSampleRate = 44100;

        /// <summary>
        /// 오디오 채널 수입니다.
        /// </summary>
        public const int AudioChannels = 2;

        /// <summary>
        /// 알림 표시 시간(밀리초)입니다.
        /// </summary>
        public const int NotificationTimeout = 5000;
    }

    /// <summary>
    /// 애플리케이션의 진입점을 제공하는 클래스입니다.
    /// </summary>
    static class Program
    {
        /// <summary>
        /// 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new SpeakerSleepGuardContext());
        }
    }

    /// <summary>
    /// 스피커를 활성 상태로 유지하는 애플리케이션의 컨텍스트 클래스입니다.
    /// </summary>
    public class SpeakerSleepGuardContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private IWavePlayer wavePlayer;
        private SilenceProvider silenceProvider;
        private bool isPlaying = false;
        private ToolStripMenuItem playStopMenuItem;

        /// <summary>
        /// SpeakerSleepGuardContext 새 인스턴스를 초기화합니다.
        /// </summary>
        public SpeakerSleepGuardContext()
        {
            InitializeTrayIcon();
            InitializeContextMenu();
            
            // 시작 시 자동 재생
            StartPlayback();
            
            // 시스템 시작에서 자동으로 실행된 것이 아니라면 알림 표시
            if (!IsStartedWithSystem())
            {
                ShowNotification();
            }
        }

        /// <summary>
        /// 트레이 아이콘을 초기화합니다.
        /// </summary>
        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon()
            {
                Icon = Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName),
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true,
                Text = Constants.DisplayName
            };

            // 트레이 아이콘 더블 클릭
            trayIcon.DoubleClick += (s, e) => TogglePlayback(s, e);
        }

        /// <summary>
        /// 컨텍스트 메뉴를 초기화합니다.
        /// </summary>
        private void InitializeContextMenu()
        {
            // 메뉴 아이템 추가
            playStopMenuItem = new ToolStripMenuItem("재생 시작");
            playStopMenuItem.Click += TogglePlayback;
            
            var startupMenuItem = new ToolStripMenuItem("시작 시 자동 실행");
            startupMenuItem.Click += ToggleStartup;
            startupMenuItem.Checked = IsStartupEnabled();
            
            var exitMenuItem = new ToolStripMenuItem("종료");
            exitMenuItem.Click += Exit;

            // 메뉴에 아이템 추가
            trayIcon.ContextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                playStopMenuItem,
                startupMenuItem,
                new ToolStripSeparator(),
                exitMenuItem
            });
        }

        /// <summary>
        /// 재생 상태를 토글합니다.
        /// </summary>
        /// <param name="sender">이벤트를 발생시킨 객체</param>
        /// <param name="e">이벤트 데이터</param>
        private void TogglePlayback(object sender, EventArgs e)
        {
            if (isPlaying)
                StopPlayback();
            else
                StartPlayback();
        }

        /// <summary>
        /// 오디오 재생을 시작합니다.
        /// </summary>
        private void StartPlayback()
        {
            if (isPlaying) return;

            try
            {
                wavePlayer = new WaveOutEvent();
                silenceProvider = new SilenceProvider(
                    WaveFormat.CreateIeeeFloatWaveFormat(Constants.AudioSampleRate, Constants.AudioChannels));
                wavePlayer.Init(silenceProvider);
                wavePlayer.Play();
                isPlaying = true;
                playStopMenuItem.Text = "재생 중지";
                trayIcon.Text = $"{Constants.DisplayName} (재생중)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오디오 재생 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 오디오 재생을 중지합니다.
        /// </summary>
        private void StopPlayback()
        {
            if (!isPlaying) return;

            try 
            {
                wavePlayer?.Stop();
                wavePlayer?.Dispose();
                wavePlayer = null;
                isPlaying = false;
                playStopMenuItem.Text = "재생 시작";
                trayIcon.Text = $"{Constants.DisplayName} (중지됨)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오디오 중지 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 시작 프로그램에 등록되어 있는지 확인합니다.
        /// </summary>
        /// <returns>시작 프로그램에 등록되어 있으면 true, 그렇지 않으면 false</returns>
        private bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key?.GetValue(Constants.ApplicationName) != null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"시작 프로그램 설정 확인 중 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 시작 프로그램 등록 상태를 토글합니다.
        /// </summary>
        /// <param name="sender">이벤트를 발생시킨 객체</param>
        /// <param name="e">이벤트 데이터</param>
        private void ToggleStartup(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            if (menuItem == null) return;

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key == null) return;

                    if (menuItem.Checked)
                    {
                        key.DeleteValue(Constants.ApplicationName, false);
                        menuItem.Checked = false;
                    }
                    else
                    {
                        key.SetValue(Constants.ApplicationName, Application.ExecutablePath);
                        menuItem.Checked = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"시작 프로그램 설정 중 오류가 발생했습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 애플리케이션을 종료합니다.
        /// </summary>
        /// <param name="sender">이벤트를 발생시킨 객체</param>
        /// <param name="e">이벤트 데이터</param>
        private void Exit(object sender, EventArgs e)
        {
            StopPlayback();
            trayIcon.Visible = false;
            Application.Exit();
        }

        /// <summary>
        /// 애플리케이션이 시스템 시작과 함께 실행되었는지 확인합니다.
        /// </summary>
        /// <returns>시스템 시작과 함께 실행되었으면 true, 그렇지 않으면 false</returns>
        private bool IsStartedWithSystem()
        {
            try
            {
                // 현재 프로세스의 시작 시간 가져오기
                DateTime processStartTime = Process.GetCurrentProcess().StartTime;
                
                // 시스템 부팅 시간 가져오기
                var query = new SelectQuery("SELECT LastBootUpTime FROM Win32_OperatingSystem");
                var searcher = new ManagementObjectSearcher(query);
                
                foreach (ManagementObject mo in searcher.Get())
                {
                    string dtString = mo["LastBootUpTime"].ToString();
                    if (DateTime.TryParseExact(
                        dtString.Substring(0, 21),
                        "yyyyMMddHHmmss.ffffff",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out DateTime bootTime))
                    {
                        // 부팅 후 지정된 시간 이내에 시작되었다면 시스템 시작과 함께 실행된 것으로 간주
                        TimeSpan timeSinceBoot = processStartTime - bootTime;
                        return timeSinceBoot.TotalMinutes < Constants.SystemStartupThresholdMinutes;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"시스템 시작 확인 중 오류: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 알림을 표시합니다.
        /// </summary>
        private void ShowNotification()
        {
            try
            {
                trayIcon.BalloonTipTitle = Constants.DisplayName;
                trayIcon.BalloonTipText = 
                    "트레이 아이콘을 더블클릭하여 재생/중지하거나 오른쪽 클릭하여 메뉴를 사용하세요.";
                trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                trayIcon.ShowBalloonTip(Constants.NotificationTimeout);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"알림 표시 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 리소스를 해제합니다.
        /// </summary>
        /// <param name="disposing">명시적으로 해제하는 경우 true, 그렇지 않으면 false</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopPlayback();
                trayIcon?.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 무음 샘플을 생성하는 클래스입니다.
    /// </summary>
    public class SilenceProvider : ISampleProvider
    {
        private readonly WaveFormat waveFormat;

        /// <summary>
        /// SilenceProvider의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="waveFormat">사용할 오디오 파형 형식</param>
        public SilenceProvider(WaveFormat waveFormat)
        {
            this.waveFormat = waveFormat;
        }

        /// <summary>
        /// 오디오 파형 형식을 가져옵니다.
        /// </summary>
        public WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// 지정된 버퍼에 무음 샘플을 채웁니다.
        /// </summary>
        /// <param name="buffer">채울 버퍼</param>
        /// <param name="offset">버퍼에서의 시작 위치</param>
        /// <param name="count">채울 샘플 수</param>
        /// <returns>채워진 샘플 수</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            // 모든 샘플을 0으로 채워 무음 생성
            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] = 0;
            }
            return count;
        }
    }
}