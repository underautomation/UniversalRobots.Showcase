﻿using System;
using System.Reflection;
using System.Windows.Forms;
using UnderAutomation.UniversalRobots;

public partial class ProgramControl : UserControl, IUserControl
{
    private UR _ur;

    public ProgramControl(UR ur)
    {
        _ur = ur;

        InitializeComponent();

        // prevent list view from flickering
        var method = typeof(Control).GetMethod("SetStyle", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Invoke(lstVariables, new object[] { ControlStyles.OptimizedDoubleBuffer, true });
        method.Invoke(lstThreads, new object[] { ControlStyles.OptimizedDoubleBuffer, true });
    }

    #region IUserControl
    public string Title => "Running program and variables";

    public bool FeatureEnabled => _ur.PrimaryInterface.Connected || (_ur.Sftp.Connected && _ur.Dashboard.Initialized);

    public void PeriodicUpdate()
    {
        lblWarningVariables.Visible = !_ur.PrimaryInterface.Connected;

        var variables = _ur.PrimaryInterface.GlobalVariables.GetAll();

        lstVariables.UpdateList(variables, v => v.Name, v => v.Type.ToString(), v => v.ToString());

        var threads = _ur.PrimaryInterface.ProgramThreads?.Threads;

        if (threads is object)
            lstThreads.UpdateList(threads, t => t.LineNumber.ToString(), t => t.LineName);
    }

    public void OnClose() { }
    public void OnOpen()
    {
        cbPrograms.Items.Clear();

        var enabled = _ur.Sftp.Connected && _ur.Dashboard.Initialized;

        btnPause.Enabled = enabled;
        btnRun.Enabled = enabled;
        btnStop.Enabled = enabled;
        cbPrograms.Enabled = enabled;

        if (!enabled)
        {
            lblPathToPrograms.Text = "Enable Sftp and dashboard to list available programs";
            return;
        }

        try
        {
            var programs = _ur.Sftp.EnumeratePrograms();
            cbPrograms.Items.AddRange(programs);
            lblPathToPrograms.Text = "";
        }
        catch (Exception e)
        {
            lblPathToPrograms.Text = $"{lblPathToPrograms.Text} ({e.Message})";
        }
    }
    #endregion

    private void btnRun_Click(object sender, System.EventArgs e)
    {
        var result = _ur.Dashboard.LoadProgram(cbPrograms.Text);

        if (!result.Succeed) throw new Exception(result.Message);

        result = _ur.Dashboard.Play();

        if (!result.Succeed) throw new Exception(result.Message);
    }

    private void btnPause_Click(object sender, EventArgs e)
    {
        _ur.Dashboard.Pause();
    }

    private void btnStop_Click(object sender, EventArgs e)
    {
        _ur.Dashboard.Stop();
    }
}