using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace DichotomyApp
{
  public class Form1 : Form
  {
    private TextBox _textBoxA, _textBoxB, _textBoxE, _textBoxFormula;
    private PictureBox _pictureGraph;
    private Label _labelResult;
    private MenuStrip _menuStrip;
    private System.Windows.Forms.Timer _autoCalcTimer;

    private double _leftBound, _rightBound, _precision;
    private FunctionParser _parser;
    private DichotomyResult _dichotomyResult;

    // Кэш GDI-ресурсов
    private Font _fontGrid, _fontLabel, _fontRoot, _fontHint;
    private Pen _penAxis, _penGrid, _penCurve, _penBounds, _penRoot;
    private SolidBrush _brushGray, _brushRed;

    public Form1()
    {
      InitializeResources();
      BuildUserInterface();

      _autoCalcTimer = new System.Windows.Forms.Timer { Interval = 250 };
      _autoCalcTimer.Tick += (sender, args) => { _autoCalcTimer.Stop(); TryAutoCalculate(); };

      _textBoxA.TextChanged += (sender, args) => ScheduleAutoCalculate();
      _textBoxB.TextChanged += (sender, args) => ScheduleAutoCalculate();
      _textBoxE.TextChanged += (sender, args) => ScheduleAutoCalculate();
      _textBoxFormula.TextChanged += (sender, args) => ScheduleAutoCalculate();

      this.FormClosed += (sender, args) => DisposeResources();
    }

    private void InitializeResources()
    {
      _fontGrid = new Font("Arial", 7);
      _fontLabel = new Font("Arial", 9, FontStyle.Bold);
      _fontRoot = new Font("Arial", 9, FontStyle.Bold);
      _fontHint = new Font("Consolas", 10, FontStyle.Bold);

      _penAxis = new Pen(Color.LightGray, 1);
      _penGrid = new Pen(Color.FromArgb(230, 230, 230), 1);
      _penCurve = new Pen(Color.SteelBlue, 2);
      _penBounds = new Pen(Color.Orange, 1.5f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
      _penRoot = new Pen(Color.Red, 2);

      _brushGray = new SolidBrush(Color.Gray);
      _brushRed = new SolidBrush(Color.Red);
    }

    private void DisposeResources()
    {
      _fontGrid?.Dispose();
      _fontLabel?.Dispose();
      _fontRoot?.Dispose();
      _fontHint?.Dispose();
      _penAxis?.Dispose();
      _penGrid?.Dispose();
      _penCurve?.Dispose();
      _penBounds?.Dispose();
      _penRoot?.Dispose();
      _brushGray?.Dispose();
      _brushRed?.Dispose();
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
      var buildGraphItem = new ToolStripMenuItem("Построить график");
      var clearItem = new ToolStripMenuItem("Очистить");
      var exitItem = new ToolStripMenuItem("Выход");

      calculateItem.Click += (sender, args) => Calculate();
      buildGraphItem.Click += (sender, args) => DrawGraph();
      clearItem.Click += (sender, args) => ClearAll();
      exitItem.Click += (sender, args) => this.Close();

      _menuStrip.Items.Add(calculateItem);
      _menuStrip.Items.Add(buildGraphItem);
      _menuStrip.Items.Add(clearItem);
      _menuStrip.Items.Add(exitItem);
      this.MainMenuStrip = _menuStrip;
      this.Controls.Add(_menuStrip);

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

      _pictureGraph = new PictureBox
      {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = Color.White
      };
      _pictureGraph.Paint += PictureGraph_Paint;
      _pictureGraph.Resize += (sender, args) => _pictureGraph.Invalidate();
      rootLayout.Controls.Add(_pictureGraph, 1, 0);
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
      _pictureGraph.Invalidate();
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
      _pictureGraph.Invalidate();
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
        _pictureGraph.Invalidate();
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
        _pictureGraph.Invalidate();
        return;
      }

      try
      {
        _dichotomyResult = DichotomySolver.Solve(_parser.Evaluate, _leftBound, _rightBound, _precision);
        ShowResult();
      }
      catch
      {
        _dichotomyResult = null;
      }
      _pictureGraph.Invalidate();
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

    private void DrawGraph()
    {
      if (_parser == null)
      {
        try
        {
          if (!TryParseDouble(_textBoxA.Text, out _leftBound))
          {
            MessageBox.Show("Введите корректное a");
            return;
          }
          if (!TryParseDouble(_textBoxB.Text, out _rightBound))
          {
            MessageBox.Show("Введите корректное b");
            return;
          }
          if (string.IsNullOrWhiteSpace(_textBoxFormula.Text))
          {
            MessageBox.Show("Введите формулу");
            return;
          }
          _parser = new FunctionParser(_textBoxFormula.Text);
        }
        catch (Exception exception)
        {
          MessageBox.Show("Ошибка формулы: " + exception.Message);
          return;
        }
      }
      _pictureGraph.Invalidate();
    }

    private void PictureGraph_Paint(object sender, PaintEventArgs e)
    {
      var graphics = e.Graphics;
      graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      graphics.Clear(Color.White);

      if (_parser == null)
      {
        using (var placeholderFont = new Font("Arial", 11, FontStyle.Italic))
        {
          var placeholderText = "Здесь появится график";
          var textSize = graphics.MeasureString(placeholderText, placeholderFont);
          graphics.DrawString(placeholderText, placeholderFont, _brushGray,
              (_pictureGraph.Width - textSize.Width) / 2,
              (_pictureGraph.Height - textSize.Height) / 2);
        }
        return;
      }

      int pictureWidth = _pictureGraph.Width;
      int pictureHeight = _pictureGraph.Height;
      int padding = 45;

      double margin = (_rightBound - _leftBound) * 0.5;
      if (margin < 1e-9) margin = 1.0;

      double xMin = _leftBound - margin;
      double xMax = _rightBound + margin;

      const int sampleCount = 800;
      double[] ySamples = new double[sampleCount + 1];
      double yMin = double.MaxValue;
      double yMax = double.MinValue;

      for (int sampleIndex = 0; sampleIndex <= sampleCount; ++sampleIndex)
      {
        double x = xMin + (xMax - xMin) * sampleIndex / sampleCount;
        double y;
        try { y = _parser.Evaluate(x); }
        catch { y = double.NaN; }

        ySamples[sampleIndex] = y;
        if (!double.IsNaN(y) && !double.IsInfinity(y) && Math.Abs(y) < 1e6)
        {
          if (y < yMin) yMin = y;
          if (y > yMax) yMax = y;
        }
      }

      if (yMin > yMax) { yMin = -10; yMax = 10; }
      if (Math.Abs(yMax - yMin) < 1e-9) { yMin -= 1; yMax += 1; }

      double yPadding = (yMax - yMin) * 0.15;
      yMin -= yPadding;
      yMax += yPadding;

      Func<double, float> toScreenX = x =>
          (float)(padding + (x - xMin) / (xMax - xMin) * (pictureWidth - 2 * padding));
      Func<double, float> toScreenY = y =>
          (float)(pictureHeight - padding - (y - yMin) / (yMax - yMin) * (pictureHeight - 2 * padding));

      if (yMin <= 0 && yMax >= 0)
        graphics.DrawLine(_penAxis, padding, toScreenY(0), pictureWidth - padding, toScreenY(0));
      if (xMin <= 0 && xMax >= 0)
        graphics.DrawLine(_penAxis, toScreenX(0), padding, toScreenX(0), pictureHeight - padding);

      for (int gridIndex = 0; gridIndex <= 10; ++gridIndex)
      {
        double gridX = xMin + (xMax - xMin) * gridIndex / 10.0;
        graphics.DrawLine(_penGrid, toScreenX(gridX), padding, toScreenX(gridX), pictureHeight - padding);
        graphics.DrawString(gridX.ToString("0.###"), _fontGrid, _brushGray,
            toScreenX(gridX) - 12, pictureHeight - padding + 2);

        double gridY = yMin + (yMax - yMin) * gridIndex / 10.0;
        graphics.DrawLine(_penGrid, padding, toScreenY(gridY), pictureWidth - padding, toScreenY(gridY));
        graphics.DrawString(gridY.ToString("0.###"), _fontGrid, _brushGray, 2, toScreenY(gridY) - 6);
      }

      PointF? previousPoint = null;
      for (int sampleIndex = 0; sampleIndex <= sampleCount; ++sampleIndex)
      {
        double x = xMin + (xMax - xMin) * sampleIndex / sampleCount;
        double y = ySamples[sampleIndex];

        if (double.IsNaN(y) || double.IsInfinity(y) || Math.Abs(y) > 1e6)
        {
          previousPoint = null;
          continue;
        }

        var currentPoint = new PointF(toScreenX(x), toScreenY(y));
        if (currentPoint.Y < padding - 200 || currentPoint.Y > pictureHeight - padding + 200)
        {
          previousPoint = null;
          continue;
        }

        if (previousPoint.HasValue)
          graphics.DrawLine(_penCurve, previousPoint.Value, currentPoint);
        previousPoint = currentPoint;
      }

      graphics.DrawLine(_penBounds, toScreenX(_leftBound), padding, toScreenX(_leftBound), pictureHeight - padding);
      graphics.DrawLine(_penBounds, toScreenX(_rightBound), padding, toScreenX(_rightBound), pictureHeight - padding);

      if (_dichotomyResult != null
          && _dichotomyResult.FoundRight > _dichotomyResult.FoundLeft
          && _dichotomyResult.FoundRight - _dichotomyResult.FoundLeft < (_rightBound - _leftBound) * 0.9)
      {
        using (var foundRangePen = new Pen(Color.FromArgb(120, 0, 128, 0), 4))
        {
          graphics.DrawLine(foundRangePen,
              toScreenX(_dichotomyResult.FoundLeft), padding + 2,
              toScreenX(_dichotomyResult.FoundRight), padding + 2);
        }
      }

      if (_dichotomyResult != null && _dichotomyResult.Success)
      {
        float rootScreenX = toScreenX(_dichotomyResult.Root);
        float axisScreenY = toScreenY(0);

        graphics.DrawLine(_penRoot, rootScreenX, axisScreenY - 8, rootScreenX, axisScreenY + 8);
        graphics.DrawLine(_penRoot, rootScreenX - 8, axisScreenY, rootScreenX + 8, axisScreenY);
        graphics.FillEllipse(_brushRed, rootScreenX - 5, axisScreenY - 5, 10, 10);
        graphics.DrawString($"x = {_dichotomyResult.Root:F5}", _fontRoot, _brushRed,
            rootScreenX + 6, axisScreenY - 22);
      }

      graphics.DrawString($"f(x) = {_textBoxFormula.Text}", _fontHint, Brushes.Black, padding, 5);
    }
  }
}