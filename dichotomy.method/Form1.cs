using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using ScottPlot.WinForms;

namespace DichotomyApp
{
  public class Form1 : Form
  {
    private TextBox _textBoxA;
    private TextBox _textBoxB;
    private TextBox _textBoxE;
    private TextBox _textBoxFormula;

    private FormsPlot _formsPlot;

    private Label _labelResult;

    private Label _valueRoot;
    private Label _valueFunction;
    private Label _valueIterations;
    private Label _valuePrecision;

    private TableLayoutPanel _resultTable;
    private TableLayoutPanel _leftPanel;

    private MenuStrip _menuStrip;
    private Label _helpIcon;
    private ToolTip _formulaToolTip;

    private System.Windows.Forms.Timer _autoCalcTimer;

    private double _leftBound;
    private double _rightBound;
    private double _precision;

    private FunctionParser _parser;
    private DichotomyResult _dichotomyResult;

    public Form1()
    {
      BuildUserInterface();

      _autoCalcTimer = new System.Windows.Forms.Timer
      {
        Interval = 250
      };

      _autoCalcTimer.Tick += (sender, args) =>
      {
        _autoCalcTimer.Stop();
        TryAutoCalculate();
      };

      _textBoxA.TextChanged += (sender, args) =>
          ScheduleAutoCalculate();

      _textBoxB.TextChanged += (sender, args) =>
          ScheduleAutoCalculate();

      _textBoxE.TextChanged += (sender, args) =>
          ScheduleAutoCalculate();

      _textBoxFormula.TextChanged += (sender, args) =>
          ScheduleAutoCalculate();
    }

    // ============================================================
    // ИНТЕРФЕЙС
    // ============================================================

    private void BuildUserInterface()
    {
      Text = "Метод дихотомии";

      Width = 1200;
      Height = 720;

      MinimumSize = new Size(950, 620);

      StartPosition =
          FormStartPosition.CenterScreen;

      Font = new Font(
          "Segoe UI",
          9F,
          FontStyle.Regular);

      // ========================================================
      // MENU STRIP
      // ========================================================

      _menuStrip = new MenuStrip();

      var calculateItem =
          new ToolStripMenuItem("Рассчитать");

      var plotItem =
          new ToolStripMenuItem("Построить график");

      var clearItem =
          new ToolStripMenuItem("Очистить");

      calculateItem.Click +=
          (sender, args) => Calculate();

      plotItem.Click +=
          (sender, args) => BuildGraphOnly();

      clearItem.Click +=
          (sender, args) => ClearAll();

      _menuStrip.Items.Add(calculateItem);
      _menuStrip.Items.Add(plotItem);
      _menuStrip.Items.Add(clearItem);

      MainMenuStrip = _menuStrip;

      Controls.Add(_menuStrip);

      // ========================================================
      // ОСНОВНОЙ LAYOUT
      // ========================================================

      var rootLayout =
          new TableLayoutPanel
          {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(14)
          };

      rootLayout.ColumnStyles.Add(
          new ColumnStyle(
              SizeType.Absolute,
              450));

      rootLayout.ColumnStyles.Add(
          new ColumnStyle(
              SizeType.Percent,
              100));

      Controls.Add(rootLayout);

      // ========================================================
      // ЛЕВАЯ ПАНЕЛЬ
      // ========================================================

      _leftPanel =
          new TableLayoutPanel
          {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 8,
            Padding = new Padding(0)
          };

      _leftPanel.ColumnStyles.Add(
          new ColumnStyle(
              SizeType.Absolute,
              100));

      _leftPanel.ColumnStyles.Add(
          new ColumnStyle(
              SizeType.Percent,
              100));

      _leftPanel.ColumnStyles.Add(
          new ColumnStyle(
              SizeType.Absolute,
              42));

      // a
      _leftPanel.RowStyles.Add(
          new RowStyle(
              SizeType.Absolute,
              40));

      // b
      _leftPanel.RowStyles.Add(
          new RowStyle(
              SizeType.Absolute,
              40));

      // e
      _leftPanel.RowStyles.Add(
          new RowStyle(
              SizeType.Absolute,
              40));

      // f(x)
      _leftPanel.RowStyles.Add(
          new RowStyle(
              SizeType.Absolute,
              40));

      // отступ
      _leftPanel.RowStyles.Add(
          new RowStyle(
              SizeType.Absolute,
              10));

      // заголовок результата — увеличено для многострочного текста
      _leftPanel.RowStyles.Add(
          new RowStyle(
              SizeType.Absolute,
              68));

      // таблица результата
      _leftPanel.RowStyles.Add(
          new RowStyle(
              SizeType.Absolute,
              116));

      // остаток
      _leftPanel.RowStyles.Add(
          new RowStyle(
              SizeType.Percent,
              100));

      // ========================================================
      // a
      // ========================================================

      _leftPanel.Controls.Add(
          MakeLabel("a ="),
          0,
          0);

      _textBoxA =
          MakeTextBox();

      _leftPanel.Controls.Add(
          _textBoxA,
          1,
          0);

      _leftPanel.SetColumnSpan(
          _textBoxA,
          2);

      // ========================================================
      // b
      // ========================================================

      _leftPanel.Controls.Add(
          MakeLabel("b ="),
          0,
          1);

      _textBoxB =
          MakeTextBox();

      _leftPanel.Controls.Add(
          _textBoxB,
          1,
          1);

      _leftPanel.SetColumnSpan(
          _textBoxB,
          2);

      // ========================================================
      // e
      // ========================================================

      _leftPanel.Controls.Add(
          MakeLabel("e ="),
          0,
          2);

      _textBoxE =
          MakeTextBox();

      _textBoxE.Text =
          "0,0001";

      _leftPanel.Controls.Add(
          _textBoxE,
          1,
          2);

      _leftPanel.SetColumnSpan(
          _textBoxE,
          2);

      // ========================================================
      // f(x)
      // ========================================================

      _leftPanel.Controls.Add(
          MakeLabel("f(x) ="),
          0,
          3);

      _textBoxFormula =
          MakeTextBox();

      _textBoxFormula.Text =
          "x^2 + 2*x - 6";

      _leftPanel.Controls.Add(
          _textBoxFormula,
          1,
          3);

      // ========================================================
      // ?
      // ========================================================

      _helpIcon =
          new Label
          {
            Text = "?",

            Dock = DockStyle.Fill,

            TextAlign =
                  ContentAlignment.MiddleCenter,

            Font = new Font(
                  "Segoe UI",
                  11F,
                  FontStyle.Bold),

            ForeColor =
                  Color.White,

            BackColor =
                  Color.SteelBlue,

            Cursor =
                  Cursors.Help,

            Margin =
                  new Padding(
                      4,
                      6,
                      0,
                      6)
          };

      _leftPanel.Controls.Add(
          _helpIcon,
          2,
          3);

      // ========================================================
      // РЕЗУЛЬТАТ
      // ========================================================

      _labelResult =
          new Label
          {
            Text = "Результат",

            Dock = DockStyle.Fill,

            AutoSize = false,

            Font = new Font(
                  "Segoe UI",
                  10F,
                  FontStyle.Bold),

            ForeColor =
                  Color.DarkGreen,

            TextAlign =
                  ContentAlignment.MiddleLeft,

            Padding =
                  new Padding(0, 4, 0, 4),

            Margin =
                  new Padding(0)
          };

      _leftPanel.Controls.Add(
          _labelResult,
          0,
          5);

      _leftPanel.SetColumnSpan(
          _labelResult,
          3);

      // ========================================================
      // ТАБЛИЦА
      // ========================================================

      _resultTable =
          CreateResultTable();

      _leftPanel.Controls.Add(
          _resultTable,
          0,
          6);

      _leftPanel.SetColumnSpan(
          _resultTable,
          3);

      // ========================================================
      // ЛЕВАЯ ПАНЕЛЬ
      // ========================================================

      rootLayout.Controls.Add(
          _leftPanel,
          0,
          0);

      // ========================================================
      // ГРАФИК
      // ========================================================

      _formsPlot =
          new FormsPlot
          {
            Dock = DockStyle.Fill
          };

      rootLayout.Controls.Add(
          _formsPlot,
          1,
          0);

      // ========================================================
      // TOOLTIP
      // ========================================================

      _formulaToolTip =
          new ToolTip
          {
            AutoPopDelay = 60000,
            InitialDelay = 150,
            ReshowDelay = 50,
            ShowAlways = true,
            IsBalloon = false,
            ToolTipTitle =
                  "Поддерживаемые формулы"
          };

      _formulaToolTip.SetToolTip(
          _helpIcon,
          BuildFormulaTooltipText());
    }

    // ============================================================
    // ТАБЛИЦА РЕЗУЛЬТАТА
    // ============================================================

    private TableLayoutPanel CreateResultTable()
    {
      var table =
          new TableLayoutPanel
          {
            Dock = DockStyle.Fill,

            ColumnCount = 2,
            RowCount = 4,

            Margin =
                  new Padding(0),

            Padding =
                  new Padding(0)
          };

      table.ColumnStyles.Add(
          new ColumnStyle(
              SizeType.Absolute,
              120));

      table.ColumnStyles.Add(
          new ColumnStyle(
              SizeType.Percent,
              100));

      for (int i = 0; i < 4; i++)
      {
        table.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                29));
      }

      table.Controls.Add(
          CreateResultNameLabel("Корень"),
          0,
          0);

      _valueRoot =
          CreateResultValueLabel();

      table.Controls.Add(
          _valueRoot,
          1,
          0);

      table.Controls.Add(
          CreateResultNameLabel("f(x)"),
          0,
          1);

      _valueFunction =
          CreateResultValueLabel();

      table.Controls.Add(
          _valueFunction,
          1,
          1);

      table.Controls.Add(
          CreateResultNameLabel("Итераций"),
          0,
          2);

      _valueIterations =
          CreateResultValueLabel();

      table.Controls.Add(
          _valueIterations,
          1,
          2);

      table.Controls.Add(
          CreateResultNameLabel("Точность e"),
          0,
          3);

      _valuePrecision =
          CreateResultValueLabel();

      table.Controls.Add(
          _valuePrecision,
          1,
          3);

      return table;
    }

    private Label CreateResultNameLabel(
        string text)
    {
      return new Label
      {
        Text = text,

        Dock = DockStyle.Fill,

        Font = new Font(
              "Segoe UI",
              9F,
              FontStyle.Regular),

        TextAlign =
              ContentAlignment.MiddleLeft,

        Margin =
              new Padding(0)
      };
    }

    private Label CreateResultValueLabel()
    {
      return new Label
      {
        Text = "-",

        Dock = DockStyle.Fill,

        Font = new Font(
              "Segoe UI",
              9F,
              FontStyle.Regular),

        TextAlign =
              ContentAlignment.MiddleLeft,

        Margin =
              new Padding(0)
      };
    }

    // ============================================================
    // РЕЗУЛЬТАТ
    // ============================================================

    private void SetResultValues(
        string root,
        string functionValue,
        string iterations,
        string precision)
    {
      _valueRoot.Text = root;
      _valueFunction.Text = functionValue;
      _valueIterations.Text = iterations;
      _valuePrecision.Text = precision;
    }

    private void ClearResultTable()
    {
      SetResultValues(
          "-",
          "-",
          "-",
          "-");
    }

    private void ShowResultTable()
    {
      _resultTable.Visible = true;

      _leftPanel.RowStyles[6].Height =
          116;
    }

    private void HideResultTable()
    {
      _resultTable.Visible = false;

      _leftPanel.RowStyles[6].Height =
          0;
    }

    // ============================================================
    // LABEL
    // ============================================================

    private Label MakeLabel(
        string caption)
    {
      return new Label
      {
        Text = caption,

        Dock = DockStyle.Fill,

        TextAlign =
              ContentAlignment.MiddleRight,

        Font = new Font(
              "Segoe UI",
              10F,
              FontStyle.Bold),

        Padding =
              new Padding(
                  0,
                  0,
                  8,
                  0),

        Margin =
              new Padding(0)
      };
    }

    // ============================================================
    // TEXTBOX
    // ============================================================

    private TextBox MakeTextBox()
    {
      return new TextBox
      {
        Dock = DockStyle.Fill,

        Font = new Font(
              "Segoe UI",
              10F,
              FontStyle.Regular),

        Margin =
              new Padding(
                  0,
                  6,
                  0,
                  6)
      };
    }

    // ============================================================
    // TOOLTIP
    // ============================================================

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
          "Десятичный разделитель в формуле: точка.\r\n" +
          "Для a, b и e можно использовать точку или запятую.\r\n" +
          "Тригонометрия — в радианах.";
    }

    // ============================================================
    // АВТОРАСЧЁТ
    // ============================================================

    private void ScheduleAutoCalculate()
    {
      _autoCalcTimer.Stop();
      _autoCalcTimer.Start();
    }

    // ============================================================
    // ОШИБКА
    // ============================================================

    private void ShowError(
        string message)
    {
      _labelResult.ForeColor =
          Color.DarkRed;

      _labelResult.Text =
          "Ошибка: " + message;

      ClearResultTable();

      HideResultTable();
    }

    // ============================================================
    // ОЧИСТКА
    // ============================================================

    private void ClearAll()
    {
      _textBoxA.Text = "";
      _textBoxB.Text = "";
      _textBoxE.Text = "0,0001";
      _textBoxFormula.Text = "";

      ResetFieldColors();

      _labelResult.ForeColor =
          Color.DarkGreen;

      _labelResult.Text =
          "Результат";

      ClearResultTable();
      ShowResultTable();

      _dichotomyResult = null;
      _parser = null;

      _formsPlot.Plot.Clear();
      _formsPlot.Refresh();
    }

    // ============================================================
    // ЦВЕТА ПОЛЕЙ
    // ============================================================

    private void ResetFieldColors()
    {
      _textBoxA.BackColor =
          SystemColors.Window;

      _textBoxB.BackColor =
          SystemColors.Window;

      _textBoxE.BackColor =
          SystemColors.Window;

      _textBoxFormula.BackColor =
          SystemColors.Window;
    }

    private void MarkInvalid(
        TextBox textBox,
        bool isInvalid)
    {
      textBox.BackColor =
          isInvalid
              ? Color.MistyRose
              : SystemColors.Window;
    }

    // ============================================================
    // РАСЧЁТ
    // ============================================================

    private void Calculate()
    {
      ResetFieldColors();

      if (!TryParseDouble(
          _textBoxA.Text,
          out _leftBound))
      {
        MarkInvalid(
            _textBoxA,
            true);

        ShowError(
            "некорректное значение a.");

        return;
      }

      if (!TryParseDouble(
          _textBoxB.Text,
          out _rightBound))
      {
        MarkInvalid(
            _textBoxB,
            true);

        ShowError(
            "некорректное значение b.");

        return;
      }

      if (!TryParseDouble(
          _textBoxE.Text,
          out _precision))
      {
        MarkInvalid(
            _textBoxE,
            true);

        ShowError(
            "некорректное значение e.");

        return;
      }

      if (_leftBound >= _rightBound)
      {
        MarkInvalid(
            _textBoxA,
            true);

        MarkInvalid(
            _textBoxB,
            true);

        ShowError(
            "должно выполняться a < b.");

        return;
      }

      if (_precision <= 0)
      {
        MarkInvalid(
            _textBoxE,
            true);

        ShowError(
            "точность e должна быть больше 0.");

        return;
      }

      if (string.IsNullOrWhiteSpace(
          _textBoxFormula.Text))
      {
        MarkInvalid(
            _textBoxFormula,
            true);

        ShowError(
            "введите формулу.");

        return;
      }

      try
      {
        _parser =
            new FunctionParser(
                _textBoxFormula.Text);

        _parser.Evaluate(
            _leftBound);

        _parser.Evaluate(
            _rightBound);
      }
      catch (Exception exception)
      {
        MarkInvalid(
            _textBoxFormula,
            true);

        ShowError(
            "ошибка формулы: " +
            exception.Message);

        return;
      }

      _dichotomyResult =
          DichotomySolver.Solve(
              _parser.Evaluate,
              _leftBound,
              _rightBound,
              _precision);

      ShowResult();
      UpdatePlot();
    }

    // ============================================================
    // АВТОМАТИЧЕСКИЙ РАСЧЁТ
    // ============================================================

    private void TryAutoCalculate()
    {
      ResetFieldColors();

      if (string.IsNullOrWhiteSpace(
              _textBoxA.Text) ||
          string.IsNullOrWhiteSpace(
              _textBoxB.Text) ||
          string.IsNullOrWhiteSpace(
              _textBoxE.Text) ||
          string.IsNullOrWhiteSpace(
              _textBoxFormula.Text))
      {
        _dichotomyResult = null;
        _parser = null;

        _labelResult.ForeColor =
            Color.DarkGreen;

        _labelResult.Text =
            "Результат";

        ClearResultTable();
        ShowResultTable();

        _formsPlot.Plot.Clear();
        _formsPlot.Refresh();

        return;
      }

      if (!TryParseDouble(
          _textBoxA.Text,
          out _leftBound))
      {
        MarkInvalid(
            _textBoxA,
            true);

        ShowError(
            "некорректное значение a.");

        return;
      }

      if (!TryParseDouble(
          _textBoxB.Text,
          out _rightBound))
      {
        MarkInvalid(
            _textBoxB,
            true);

        ShowError(
            "некорректное значение b.");

        return;
      }

      if (!TryParseDouble(
          _textBoxE.Text,
          out _precision))
      {
        MarkInvalid(
            _textBoxE,
            true);

        ShowError(
            "некорректное значение e.");

        return;
      }

      if (_leftBound >= _rightBound)
      {
        MarkInvalid(
            _textBoxA,
            true);

        MarkInvalid(
            _textBoxB,
            true);

        ShowError(
            "должно выполняться a < b.");

        return;
      }

      if (_precision <= 0)
      {
        MarkInvalid(
            _textBoxE,
            true);

        ShowError(
            "точность e должна быть больше 0.");

        return;
      }

      try
      {
        _parser =
            new FunctionParser(
                _textBoxFormula.Text);

        MarkInvalid(
            _textBoxFormula,
            false);
      }
      catch (Exception exception)
      {
        MarkInvalid(
            _textBoxFormula,
            true);

        ShowError(
            "ошибка формулы: " +
            exception.Message);

        return;
      }

      try
      {
        _dichotomyResult =
            DichotomySolver.Solve(
                _parser.Evaluate,
                _leftBound,
                _rightBound,
                _precision);

        ShowResult();
        UpdatePlot();
      }
      catch (Exception exception)
      {
        _dichotomyResult = null;

        ShowError(
            exception.Message);
      }
    }

    // ============================================================
    // ПОКАЗ РЕЗУЛЬТАТА
    // ============================================================

    private void ShowResult()
    {
      if (_dichotomyResult == null)
        return;

      if (!_dichotomyResult.Success)
      {
        _labelResult.ForeColor =
            Color.DarkRed;

        _labelResult.Text =
            "Ошибка: " +
            _dichotomyResult.Error;

        ClearResultTable();

        HideResultTable();

        return;
      }

      _labelResult.ForeColor =
          Color.DarkGreen;

      _labelResult.Text =
          "Результат";

      SetResultValues(
          _dichotomyResult.Root.ToString(
              "F6",
              CultureInfo.InvariantCulture),

          _dichotomyResult.FunctionValueAtRoot
              .ToString(
                  "F8",
                  CultureInfo.InvariantCulture),

          _dichotomyResult.Iterations
              .ToString(
                  CultureInfo.InvariantCulture),

          _precision.ToString(
              "G",
              CultureInfo.InvariantCulture));

      ShowResultTable();

      if (_dichotomyResult.HasDiscontinuity)
      {
        _labelResult.Text =
            "Результат ⚠";
      }
    }

    // ============================================================
    // ПАРСИНГ ЧИСЕЛ
    // ============================================================

    private static bool TryParseDouble(
        string text,
        out double value)
    {
      text =
          (text ?? "")
          .Trim()
          .Replace(',', '.');

      return double.TryParse(
          text,
          NumberStyles.Float,
          CultureInfo.InvariantCulture,
          out value);
    }

    // ============================================================
    // ПОСТРОИТЬ ГРАФИК
    // ============================================================

    private void BuildGraphOnly()
    {
      ResetFieldColors();

      if (!TryParseDouble(
          _textBoxA.Text,
          out _leftBound))
      {
        MarkInvalid(
            _textBoxA,
            true);

        ShowError(
            "некорректное значение a.");

        return;
      }

      if (!TryParseDouble(
          _textBoxB.Text,
          out _rightBound))
      {
        MarkInvalid(
            _textBoxB,
            true);

        ShowError(
            "некорректное значение b.");

        return;
      }

      if (!TryParseDouble(
          _textBoxE.Text,
          out _precision))
      {
        MarkInvalid(
            _textBoxE,
            true);

        ShowError(
            "некорректное значение e.");

        return;
      }

      if (_leftBound >= _rightBound)
      {
        MarkInvalid(
            _textBoxA,
            true);

        MarkInvalid(
            _textBoxB,
            true);

        ShowError(
            "должно выполняться a < b.");

        return;
      }

      if (_precision <= 0)
      {
        MarkInvalid(
            _textBoxE,
            true);

        ShowError(
            "точность e должна быть больше 0.");

        return;
      }

      if (string.IsNullOrWhiteSpace(
          _textBoxFormula.Text))
      {
        MarkInvalid(
            _textBoxFormula,
            true);

        ShowError(
            "введите формулу.");

        return;
      }

      try
      {
        _parser =
            new FunctionParser(
                _textBoxFormula.Text);

        MarkInvalid(
            _textBoxFormula,
            false);
      }
      catch (Exception exception)
      {
        MarkInvalid(
            _textBoxFormula,
            true);

        ShowError(
            "ошибка формулы: " +
            exception.Message);

        return;
      }

      UpdatePlot();
    }

    // ============================================================
    // ГРАФИК
    // ============================================================

    private void UpdatePlot()
    {
      _formsPlot.Plot.Clear();

      if (_parser == null)
      {
        _formsPlot.Plot.Title(
            "График не построен");

        _formsPlot.Refresh();

        return;
      }

      double margin =
          (_rightBound - _leftBound) * 0.5;

      if (margin < 1e-9)
        margin = 1.0;

      double xMin =
          _leftBound - margin;

      double xMax =
          _rightBound + margin;

      const int sampleCount = 800;

      double[] xs =
          new double[sampleCount + 1];

      double[] ys =
          new double[sampleCount + 1];

      for (int i = 0;
           i <= sampleCount;
           i++)
      {
        double x =
            xMin +
            (xMax - xMin) *
            i /
            sampleCount;

        xs[i] = x;

        try
        {
          double y =
              _parser.Evaluate(x);

          if (double.IsNaN(y) ||
              double.IsInfinity(y))
          {
            ys[i] =
                double.NaN;
          }
          else
          {
            ys[i] = y;
          }
        }
        catch
        {
          ys[i] =
              double.NaN;
        }
      }

      // ========================================================
      // РАЗРЫВ ЛИНИИ В ТОЧКАХ СКАЧКА
      // ========================================================
      //
      // ScottPlot рисует одну непрерывную линию по всем точкам
      // массива. Если функция уходит в ±∞ (полюс), между соседними
      // сэмплами получается "вертикальная соединялка". Вставляем
      // NaN там, где скачок |Δy| аномально большой — линия рвётся.

      double[] deltas = new double[sampleCount];
      int deltaCount = 0;

      for (int i = 1; i <= sampleCount; i++)
      {
        if (!double.IsNaN(ys[i]) && !double.IsNaN(ys[i - 1]))
        {
          deltas[deltaCount++] = Math.Abs(ys[i] - ys[i - 1]);
        }
      }

      if (deltaCount > 0)
      {
        Array.Sort(deltas, 0, deltaCount);

        double medianDelta = deltas[deltaCount / 2];

        double maxAbsY = 0;
        for (int i = 0; i <= sampleCount; i++)
        {
          if (!double.IsNaN(ys[i]) && !double.IsInfinity(ys[i]))
          {
            double a = Math.Abs(ys[i]);
            if (a > maxAbsY) maxAbsY = a;
          }
        }

        double jumpThreshold = Math.Max(
            medianDelta * 20.0,
            maxAbsY * 0.5);

        if (jumpThreshold < 1e-9) jumpThreshold = 1e-9;

        for (int i = 1; i <= sampleCount; i++)
        {
          if (double.IsNaN(ys[i]) || double.IsNaN(ys[i - 1]))
            continue;

          double jump = Math.Abs(ys[i] - ys[i - 1]);

          if (jump > jumpThreshold)
          {
            ys[i] = double.NaN;
          }
        }
      }

      var scatter =
          _formsPlot.Plot.Add.Scatter(
              xs,
              ys);

      scatter.Color =
          ScottPlot.Color.FromSDColor(
              Color.SteelBlue);

      scatter.LineWidth = 2;
      scatter.MarkerSize = 0;

      // --------------------------------------------------------
      // a
      // --------------------------------------------------------

      var leftLine =
          _formsPlot.Plot.Add.VerticalLine(
              _leftBound);

      leftLine.Color =
          ScottPlot.Color
              .FromSDColor(
                  Color.Orange)
              .WithAlpha(0.6);

      leftLine.LinePattern =
          ScottPlot.LinePattern.Dashed;

      // --------------------------------------------------------
      // b
      // --------------------------------------------------------

      var rightLine =
          _formsPlot.Plot.Add.VerticalLine(
              _rightBound);

      rightLine.Color =
          ScottPlot.Color
              .FromSDColor(
                  Color.Orange)
              .WithAlpha(0.6);

      rightLine.LinePattern =
          ScottPlot.LinePattern.Dashed;

      // --------------------------------------------------------
      // Изоляционный интервал
      // --------------------------------------------------------

      if (_dichotomyResult != null &&
          _dichotomyResult.FoundRight >
          _dichotomyResult.FoundLeft &&
          _dichotomyResult.FoundRight -
          _dichotomyResult.FoundLeft <
          (_rightBound -
           _leftBound) * 0.9)
      {
        var foundRange =
            _formsPlot.Plot.Add.HorizontalSpan(
                _dichotomyResult.FoundLeft,
                _dichotomyResult.FoundRight);

        foundRange.FillColor =
            ScottPlot.Color
                .FromSDColor(
                    Color.Green)
                .WithAlpha(0.2);
      }

      // --------------------------------------------------------
      // Корень
      // --------------------------------------------------------

      if (_dichotomyResult != null &&
          _dichotomyResult.Success)
      {
        var rootMarker =
            _formsPlot.Plot.Add.Marker(
                _dichotomyResult.Root,
                _dichotomyResult.FunctionValueAtRoot);

        rootMarker.Color =
            ScottPlot.Color.FromSDColor(
                Color.Red);

        rootMarker.Size = 12;
      }

      _formsPlot.Plot.Title(
          $"f(x) = {_textBoxFormula.Text}");

      _formsPlot.Plot.Axes.AutoScale();

      _formsPlot.Refresh();
    }
  }
}