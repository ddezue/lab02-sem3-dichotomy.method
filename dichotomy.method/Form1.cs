using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using ScottPlot.WinForms;

namespace DichotomyApp
{
  public class Form1 : Form
  {
    private TextBox _textBoxA, _textBoxB, _textBoxE, _textBoxFormula;
    private FormsPlot _formsPlot;
    private Label _labelResult;
    private MenuStrip _menuStrip;
    private Label _helpIcon;
    private ToolTip _formulaToolTip;
    private System.Windows.Forms.Timer _autoCalcTimer;

    private double _leftBound, _rightBound, _precision;
    private FunctionParser _parser;
    private DichotomyResult _dichotomyResult;

    public Form1()
    {
      BuildUserInterface();

      _autoCalcTimer = new System.Windows.Forms.Timer { Interval = 250 };
      _autoCalcTimer.Tick += (sender, args) => { _autoCalcTimer.Stop(); TryAutoCalculate(); };

      _textBoxA.TextChanged += (sender, args) => ScheduleAutoCalculate();
      _textBoxB.TextChanged += (sender, args) => ScheduleAutoCalculate();
      _textBoxE.TextChanged += (sender, args) => ScheduleAutoCalculate();
      _textBoxFormula.TextChanged += (sender, args) => ScheduleAutoCalculate();
    }

    private void BuildUserInterface()
    {
      this.Text = "Метод дихотомии";
      this.Width = 1200;
      this.Height = 720;
      this.MinimumSize = new Size(950, 620);
      this.StartPosition = FormStartPosition.CenterScreen;
      this.Font = new Font("Segoe UI", 9);

      // ===== Меню =====
      _menuStrip = new MenuStrip();
      var calculateItem = new ToolStripMenuItem("Рассчитать");
      var clearItem = new ToolStripMenuItem("Очистить");

      calculateItem.Click += (sender, args) => Calculate();
      clearItem.Click += (sender, args) => ClearAll();

      _menuStrip.Items.Add(calculateItem);
      _menuStrip.Items.Add(clearItem);

      this.MainMenuStrip = _menuStrip;
      this.Controls.Add(_menuStrip);

      // ===== Корневой layout =====
      var rootLayout = new TableLayoutPanel
      {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 1,
        Padding = new Padding(14)
      };
      rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420));
      rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
      this.Controls.Add(rootLayout);
      rootLayout.BringToFront();

      // ===== ЛЕВАЯ ПАНЕЛЬ =====
      var leftPanel = new TableLayoutPanel
      {
        Dock = DockStyle.Fill,
        ColumnCount = 3,
        RowCount = 7,
        Padding = new Padding(0)
      };
      leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
      leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
      leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));

      leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
      leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
      leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
      leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
      leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
      leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
      leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

      // a
      leftPanel.Controls.Add(MakeLabel("a ="), 0, 0);
      _textBoxA = MakeTextBox();
      leftPanel.Controls.Add(_textBoxA, 1, 0);
      leftPanel.SetColumnSpan(_textBoxA, 2);

      // b
      leftPanel.Controls.Add(MakeLabel("b ="), 0, 1);
      _textBoxB = MakeTextBox();
      leftPanel.Controls.Add(_textBoxB, 1, 1);
      leftPanel.SetColumnSpan(_textBoxB, 2);

      // e
      leftPanel.Controls.Add(MakeLabel("e ="), 0, 2);
      _textBoxE = MakeTextBox();
      _textBoxE.Text = "0,0001";
      leftPanel.Controls.Add(_textBoxE, 1, 2);
      leftPanel.SetColumnSpan(_textBoxE, 2);

      // f(x) + "?"
      leftPanel.Controls.Add(MakeLabel("f(x) ="), 0, 3);
      _textBoxFormula = MakeTextBox();
      _textBoxFormula.Text = "x^2 + 2*x - 6";
      leftPanel.Controls.Add(_textBoxFormula, 1, 3);

      _helpIcon = new Label
      {
        Text = "?",
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 11, FontStyle.Bold),
        ForeColor = Color.White,
        BackColor = Color.SteelBlue,
        Cursor = Cursors.Help,
        Margin = new Padding(4, 6, 0, 6)
      };
      leftPanel.Controls.Add(_helpIcon, 2, 3);

      // Результат
      _labelResult = new Label
      {
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 10, FontStyle.Bold),
        ForeColor = Color.DarkBlue,
        Text = "Введите данные и нажмите «Рассчитать»",
        Padding = new Padding(0, 12, 0, 0),
        TextAlign = ContentAlignment.TopLeft
      };
      leftPanel.Controls.Add(_labelResult, 0, 5);
      leftPanel.SetColumnSpan(_labelResult, 3);

      rootLayout.Controls.Add(leftPanel, 0, 0);

      // ===== Правая панель =====
      _formsPlot = new FormsPlot { Dock = DockStyle.Fill };
      rootLayout.Controls.Add(_formsPlot, 1, 0);

      // ===== ToolTip при наведении на "?" =====
      _formulaToolTip = new ToolTip
      {
        AutoPopDelay = 60000,
        InitialDelay = 150,
        ReshowDelay = 50,
        ShowAlways = true,
        IsBalloon = false,
        ToolTipTitle = "Поддерживаемые формулы"
      };
      _formulaToolTip.SetToolTip(_helpIcon, BuildFormulaTooltipText());
    }

    private Label MakeLabel(string caption) =>
        new Label
        {
          Text = caption,
          Dock = DockStyle.Fill,
          TextAlign = ContentAlignment.MiddleRight,
          Font = new Font("Segoe UI", 10, FontStyle.Bold),
          Padding = new Padding(0, 0, 8, 0)
        };

    private TextBox MakeTextBox() =>
        new TextBox
        {
          Dock = DockStyle.Fill,
          Font = new Font("Consolas", 11),
          Margin = new Padding(0, 7, 0, 7)
        };

    private static string BuildFormulaTooltipText()
    {
      return
          "ОПЕРАТОРЫ:\r\n" +
          "  + - * / ^  и  ( )\r\n" +
          "\r\n" +
          "ФУНКЦИИ:\r\n" +
          "  sin(x)   cos(x)   tan(x)\r\n" +
          "  exp(x)   ln(x)    log(x)\r\n" +
          "  sqrt(x)  abs(x)\r\n" +
          "\r\n" +
          "КОНСТАНТЫ:\r\n" +
          "  pi ≈ 3.14159    e ≈ 2.71828\r\n" +
          "\r\n" +
          "ПРИМЕРЫ:\r\n" +
          "  x^2 + 2*x - 6\r\n" +
          "  sin(x) - 0.5\r\n" +
          "  sqrt(x) - 1\r\n" +
          "  ln(x) - 1\r\n" +
          "\r\n" +
          "Десятичный разделитель: точка или запятая.\r\n" +
          "Тригонометрия — в радианах.";
    }

    private void ScheduleAutoCalculate()
    {
      _autoCalcTimer.Stop();
      _autoCalcTimer.Start();
    }

    private void ClearAll()
    {
      _textBoxA.Text = _textBoxB.Text = _textBoxFormula.Text = "";
      _textBoxE.Text = "0,0001";
      ResetFieldColors();
      _labelResult.ForeColor = Color.DarkBlue;
      _labelResult.Text = "Очищено";
      _dichotomyResult = null;
      _parser = null;
      _formsPlot.Plot.Clear();
      _formsPlot.Refresh();
    }

    private void ResetFieldColors()
    {
      _textBoxA.BackColor = _textBoxB.BackColor = _textBoxE.BackColor = _textBoxFormula.BackColor = SystemColors.Window;
    }

    private void MarkInvalid(TextBox textBox, bool isInvalid)
    {
      textBox.BackColor = isInvalid ? Color.MistyRose : SystemColors.Window;
    }

    private void Calculate()
    {
      ResetFieldColors();

      bool hasInvalidInput = false;
      if (!TryParseDouble(_textBoxA.Text, out _leftBound)) { MarkInvalid(_textBoxA, true); hasInvalidInput = true; }
      if (!TryParseDouble(_textBoxB.Text, out _rightBound)) { MarkInvalid(_textBoxB, true); hasInvalidInput = true; }
      if (!TryParseDouble(_textBoxE.Text, out _precision)) { MarkInvalid(_textBoxE, true); hasInvalidInput = true; }

      if (hasInvalidInput)
      {
        _labelResult.ForeColor = Color.DarkRed;
        _labelResult.Text = "Ошибка: некорректные числа в подсвеченных полях";
        return;
      }

      if (string.IsNullOrWhiteSpace(_textBoxFormula.Text))
      {
        MarkInvalid(_textBoxFormula, true);
        _labelResult.ForeColor = Color.DarkRed;
        _labelResult.Text = "Ошибка: введите формулу";
        return;
      }

      try
      {
        _parser = new FunctionParser(_textBoxFormula.Text);
        _parser.Evaluate(_leftBound);
        _parser.Evaluate(_rightBound);
      }
      catch (Exception exception)
      {
        MarkInvalid(_textBoxFormula, true);
        _labelResult.ForeColor = Color.DarkRed;
        _labelResult.Text = "Ошибка формулы: " + exception.Message;
        return;
      }

      _dichotomyResult = DichotomySolver.Solve(_parser.Evaluate, _leftBound, _rightBound, _precision);
      ShowResult();
      UpdatePlot();
    }

    private void TryAutoCalculate()
    {
      bool isAValid = TryParseDouble(_textBoxA.Text, out _leftBound);
      bool isBValid = TryParseDouble(_textBoxB.Text, out _rightBound);
      bool isEValid = TryParseDouble(_textBoxE.Text, out _precision);
      bool isFormulaValid = !string.IsNullOrWhiteSpace(_textBoxFormula.Text);

      MarkInvalid(_textBoxA, _textBoxA.Text.Length > 0 && !isAValid);
      MarkInvalid(_textBoxB, _textBoxB.Text.Length > 0 && !isBValid);
      MarkInvalid(_textBoxE, _textBoxE.Text.Length > 0 && !isEValid);

      if (!isAValid || !isBValid || !isEValid || !isFormulaValid)
      {
        _dichotomyResult = null;
        _parser = null;
        _formsPlot.Plot.Clear();
        _formsPlot.Refresh();
        return;
      }

      try
      {
        _parser = new FunctionParser(_textBoxFormula.Text);
        MarkInvalid(_textBoxFormula, false);
      }
      catch
      {
        MarkInvalid(_textBoxFormula, true);
        _parser = null;
        _dichotomyResult = null;
        _formsPlot.Plot.Clear();
        _formsPlot.Refresh();
        return;
      }

      try
      {
        _dichotomyResult = DichotomySolver.Solve(_parser.Evaluate, _leftBound, _rightBound, _precision);
        ShowResult();
        UpdatePlot();
      }
      catch
      {
        _dichotomyResult = null;
      }
    }

    private void ShowResult()
    {
      if (_dichotomyResult == null) return;

      if (!_dichotomyResult.Success)
      {
        _labelResult.ForeColor = Color.DarkRed;
        _labelResult.Text = "Ошибка: " + _dichotomyResult.Error;
        return;
      }

      _labelResult.ForeColor = Color.DarkGreen;
      _labelResult.Text = string.Format(CultureInfo.InvariantCulture,
          "Корень:   x = {0:F6}\r\nf(x)   = {1:F8}\r\nИтераций: {2}\r\ne      = {3}",
          _dichotomyResult.Root,
          _dichotomyResult.FunctionValueAtRoot,
          _dichotomyResult.Iterations,
          _precision);

      if (_dichotomyResult.HasDiscontinuity)
        _labelResult.Text += "\r\n⚠ На интервале есть разрыв — проверьте результат!";
    }

    private static bool TryParseDouble(string text, out double value)
    {
      text = (text ?? "").Trim().Replace(',', '.');
      return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private void UpdatePlot()
    {
      _formsPlot.Plot.Clear();

      if (_parser == null)
      {
        _formsPlot.Plot.Title("График не построен");
        _formsPlot.Refresh();
        return;
      }

      double margin = (_rightBound - _leftBound) * 0.5;
      if (margin < 1e-9) margin = 1.0;

      double xMin = _leftBound - margin;
      double xMax = _rightBound + margin;

      const int sampleCount = 800;
      double[] xs = new double[sampleCount + 1];
      double[] ys = new double[sampleCount + 1];

      for (int sampleIndex = 0; sampleIndex <= sampleCount; ++sampleIndex)
      {
        double x = xMin + (xMax - xMin) * sampleIndex / sampleCount;
        xs[sampleIndex] = x;
        try { ys[sampleIndex] = _parser.Evaluate(x); }
        catch { ys[sampleIndex] = double.NaN; }
      }

      var scatter = _formsPlot.Plot.Add.Scatter(xs, ys);
      scatter.Color = ScottPlot.Color.FromSDColor(System.Drawing.Color.SteelBlue);
      scatter.LineWidth = 2;
      scatter.MarkerSize = 0;

      var leftLine = _formsPlot.Plot.Add.VerticalLine(_leftBound);
      leftLine.Color = ScottPlot.Color.FromSDColor(System.Drawing.Color.Orange).WithAlpha(0.6);
      leftLine.LinePattern = ScottPlot.LinePattern.Dashed;

      var rightLine = _formsPlot.Plot.Add.VerticalLine(_rightBound);
      rightLine.Color = ScottPlot.Color.FromSDColor(System.Drawing.Color.Orange).WithAlpha(0.6);
      rightLine.LinePattern = ScottPlot.LinePattern.Dashed;

      if (_dichotomyResult != null
          && _dichotomyResult.FoundRight > _dichotomyResult.FoundLeft
          && _dichotomyResult.FoundRight - _dichotomyResult.FoundLeft < (_rightBound - _leftBound) * 0.9)
      {
        var foundRange = _formsPlot.Plot.Add.HorizontalSpan(
            _dichotomyResult.FoundLeft,
            _dichotomyResult.FoundRight);
        foundRange.FillColor = ScottPlot.Color.FromSDColor(System.Drawing.Color.Green).WithAlpha(0.2);
      }

      if (_dichotomyResult != null && _dichotomyResult.Success)
      {
        var rootMarker = _formsPlot.Plot.Add.Marker(
            _dichotomyResult.Root,
            _dichotomyResult.FunctionValueAtRoot);
        rootMarker.Color = ScottPlot.Color.FromSDColor(System.Drawing.Color.Red);
        rootMarker.Size = 12;
      }

      _formsPlot.Plot.Title($"f(x) = {_textBoxFormula.Text}");
      _formsPlot.Plot.Axes.AutoScale();
      _formsPlot.Refresh();
    }
  }
}