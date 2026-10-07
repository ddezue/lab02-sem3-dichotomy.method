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
      this.MinimumSize = new Size(900, 600);
      this.StartPosition = FormStartPosition.CenterScreen;
      this.Font = new Font("Arial", 9);

      _menuStrip = new MenuStrip();
      var calculateItem = new ToolStripMenuItem("Рассчитать");
      var clearItem = new ToolStripMenuItem("Очистить");
      var exitItem = new ToolStripMenuItem("Выход");

      calculateItem.Click += (sender, args) => Calculate();
      clearItem.Click += (sender, args) => ClearAll();
      exitItem.Click += (sender, args) => this.Close();

      _menuStrip.Items.Add(calculateItem);
      _menuStrip.Items.Add(clearItem);
      _menuStrip.Items.Add(exitItem);
      this.MainMenuStrip = _menuStrip;
      this.Controls.Add(_menuStrip);

      // ===== Корневой layout: 2 колонки =====
      var rootLayout = new TableLayoutPanel
      {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 1,
        Padding = new Padding(10)
      };
      rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420));
      rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
      this.Controls.Add(rootLayout);
      rootLayout.BringToFront();

      // ===== ЛЕВАЯ ПАНЕЛЬ: ввод =====
      var leftPanel = new TableLayoutPanel
      {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 7,
        Padding = new Padding(5)
      };
      leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
      leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

      for (int rowIndex = 0; rowIndex < 4; ++rowIndex)
        leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

      leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
      leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
      leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

      leftPanel.Controls.Add(MakeLabel("a ="), 0, 0);
      _textBoxA = MakeTextBox();
      leftPanel.Controls.Add(_textBoxA, 1, 0);

      leftPanel.Controls.Add(MakeLabel("b ="), 0, 1);
      _textBoxB = MakeTextBox();
      leftPanel.Controls.Add(_textBoxB, 1, 1);

      leftPanel.Controls.Add(MakeLabel("e ="), 0, 2);
      _textBoxE = MakeTextBox();
      _textBoxE.Text = "0,0001";
      leftPanel.Controls.Add(_textBoxE, 1, 2);

      leftPanel.Controls.Add(MakeLabel("f(x) ="), 0, 3);
      _textBoxFormula = MakeTextBox();
      _textBoxFormula.Text = "x^2 + 2*x - 6";
      leftPanel.Controls.Add(_textBoxFormula, 1, 3);

      var hintLabel = new Label
      {
        Text = "Поддерживается: + − * / ^ ( ), sin cos tan exp ln log sqrt abs, pi, e",
        Dock = DockStyle.Fill,
        ForeColor = Color.Gray,
        Font = new Font("Arial", 8, FontStyle.Italic)
      };
      leftPanel.Controls.Add(hintLabel, 0, 4);
      leftPanel.SetColumnSpan(hintLabel, 2);

      _labelResult = new Label
      {
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 10, FontStyle.Bold),
        ForeColor = Color.DarkBlue,
        Text = "Введите данные и нажмите «Рассчитать»",
        Padding = new Padding(0, 10, 0, 0)
      };
      leftPanel.Controls.Add(_labelResult, 0, 5);
      leftPanel.SetColumnSpan(_labelResult, 2);

      rootLayout.Controls.Add(leftPanel, 0, 0);

      // ===== ПРАВАЯ ПАНЕЛЬ: ScottPlot =====
      _formsPlot = new FormsPlot { Dock = DockStyle.Fill };
      rootLayout.Controls.Add(_formsPlot, 1, 0);
    }

    private Label MakeLabel(string caption) =>
        new Label
        {
          Text = caption,
          Dock = DockStyle.Fill,
          TextAlign = ContentAlignment.MiddleRight,
          Font = new Font("Arial", 10, FontStyle.Bold)
        };

    private TextBox MakeTextBox() =>
        new TextBox
        {
          Dock = DockStyle.Fill,
          Font = new Font("Consolas", 11),
          Margin = new Padding(3, 5, 3, 5)
        };

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
          "Корень: x = {0:F6}\r\nf(x) = {1:E3}\r\nИтераций: {2}\r\ne = {3}",
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

    /// <summary>
    /// Строит график через ScottPlot: кривая f(x), границы [a;b],
    /// найденный интервал и красная точка корня.
    /// </summary>
    private void UpdatePlot()
    {
      _formsPlot.Plot.Clear();

      if (_parser == null)
      {
        _formsPlot.Plot.Title("График не построен");
        _formsPlot.Refresh();
        return;
      }

      // --- 1. Кривая f(x) ---
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

      // --- 2. Границы [a;b] ---
      var leftLine = _formsPlot.Plot.Add.VerticalLine(_leftBound);
      leftLine.Color = ScottPlot.Color.FromSDColor(System.Drawing.Color.Orange).WithAlpha(0.6);
      leftLine.LinePattern = ScottPlot.LinePattern.Dashed;

      var rightLine = _formsPlot.Plot.Add.VerticalLine(_rightBound);
      rightLine.Color = ScottPlot.Color.FromSDColor(System.Drawing.Color.Orange).WithAlpha(0.6);
      rightLine.LinePattern = ScottPlot.LinePattern.Dashed;

      // --- 3. Найденный интервал ---
      if (_dichotomyResult != null
          && _dichotomyResult.FoundRight > _dichotomyResult.FoundLeft
          && _dichotomyResult.FoundRight - _dichotomyResult.FoundLeft < (_rightBound - _leftBound) * 0.9)
      {
        var foundRange = _formsPlot.Plot.Add.HorizontalSpan(
            _dichotomyResult.FoundLeft,
            _dichotomyResult.FoundRight);
        foundRange.FillColor = ScottPlot.Color.FromSDColor(System.Drawing.Color.Green).WithAlpha(0.2);
      }

      // --- 4. Точка корня ---
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