using System;
using System.Collections.Generic;

namespace DichotomyApp
{
  public class DichotomyResult
  {
    public double Root;
    public double FunctionValueAtRoot;
    public int Iterations;
    public bool Success;
    public string Error;
    public bool HasDiscontinuity;
    public double FoundLeft, FoundRight;
  }

  public static class DichotomySolver
  {
    public static DichotomyResult Solve(
        Func<double, double> function,
        double a,
        double b,
        double eps,
        int signScanSegments = 400)
    {
      var result = new DichotomyResult();

      if (double.IsNaN(a) || double.IsNaN(b) || double.IsNaN(eps))
      {
        result.Error = "Входные значения не числа";
        return result;
      }

      if (a >= b)
      {
        result.Error = "Требуется a < b";
        return result;
      }

      if (eps <= 0)
      {
        result.Error = "Точность e должна быть > 0";
        return result;
      }

      if (eps >= (b - a))
      {
        result.Error = "Точность e больше длины интервала";
        return result;
      }

      // ===== 1. Поиск интервала изоляции =====
      var signChanges = new List<(double l, double r)>();
      double step = (b - a) / signScanSegments;

      double previousX = a;
      double previousValue;

      if (!TryEvaluate(function, a, out previousValue)
          || double.IsNaN(previousValue)
          || double.IsInfinity(previousValue))
      {
        result.Error = "f(a) не вычислима";
        return result;
      }

      // Если левый конец уже является корнем
      if (Math.Abs(previousValue) < 1e-15)
      {
        signChanges.Add((a, a));
      }

      for (int scanIndex = 1; scanIndex <= signScanSegments; ++scanIndex)
      {
        double currentX =
            (scanIndex == signScanSegments)
                ? b
                : a + scanIndex * step;

        double currentValue;

        if (!TryEvaluate(function, currentX, out currentValue)
            || double.IsNaN(currentValue)
            || double.IsInfinity(currentValue))
        {
          result.HasDiscontinuity = true;

          // После разрыва старое значение нельзя использовать
          // для определения смены знака.
          previousX = currentX;
          previousValue = double.NaN;

          continue;
        }

        // Текущая точка является корнем
        if (Math.Abs(currentValue) < 1e-15)
        {
          // Не добавляем повторно тот же самый корень
          bool alreadyFound = false;

          foreach (var interval in signChanges)
          {
            if (Math.Abs(interval.l - currentX) < 1e-15 &&
                Math.Abs(interval.r - currentX) < 1e-15)
            {
              alreadyFound = true;
              break;
            }
          }

          if (!alreadyFound)
          {
            signChanges.Add((currentX, currentX));
          }
        }
        else if (!double.IsNaN(previousValue)
                 && !double.IsInfinity(previousValue))
        {
          // Обычная смена знака
          if (previousValue * currentValue < 0)
          {
            signChanges.Add((previousX, currentX));
          }
        }

        previousX = currentX;
        previousValue = currentValue;
      }

      if (signChanges.Count == 0)
      {
        if (result.HasDiscontinuity)
        {
          result.Error =
              "На интервале есть разрыв, но корень не обнаружен.";
        }
        else
        {
          result.Error =
              "Корень не обнаружен: функция не меняет знак на заданном интервале.";
        }

        return result;
      }

      // По условию должен быть выбран интервал изоляции
      // с единственным корнем.
      if (signChanges.Count > 1)
      {
        result.Error =
            $"У вас несколько корней на выбранном интервале ({signChanges.Count} шт.). Уточните [a,b].";

        return result;
      }

      double left = signChanges[0].l;
      double right = signChanges[0].r;

      result.FoundLeft = left;
      result.FoundRight = right;

      // Если корень уже точно найден в точке
      if (Math.Abs(right - left) < 1e-15)
      {
        result.Root = left;

        if (!TryEvaluate(
                function,
                left,
                out result.FunctionValueAtRoot))
        {
          result.Error = "f(left) не вычислима";
          return result;
        }

        if (Math.Abs(result.FunctionValueAtRoot) > 1e-6)
        {
          result.HasDiscontinuity = true;
          result.Error = "На интервале полюс, а не корень";
          return result;
        }

        result.Success = true;
        result.Iterations = 0;
        return result;
      }

      // ===== 2. Метод половинного деления =====
      double leftValue;

      if (!TryEvaluate(function, left, out leftValue))
      {
        result.Error = "f(left) не вычислима";
        return result;
      }

      int iterationCount = 0;
      const int maxIterations = 10000;

      while ((right - left) > eps && iterationCount < maxIterations)
      {
        ++iterationCount;

        double middle = left + (right - left) * 0.5;

        double middleValue;

        if (!TryEvaluate(function, middle, out middleValue)
            || double.IsNaN(middleValue)
            || double.IsInfinity(middleValue))
        {
          result.HasDiscontinuity = true;
          result.Error =
              "Функция не вычислима внутри интервала. Возможно, присутствует разрыв.";

          return result;
        }

        if (Math.Abs(middleValue) < 1e-15)
        {
          left = right = middle;
          break;
        }

        if (leftValue * middleValue < 0)
        {
          right = middle;
        }
        else
        {
          left = middle;
          leftValue = middleValue;
        }
      }

      if (iterationCount >= maxIterations)
      {
        result.Error =
            "Превышено максимальное число итераций";
        return result;
      }

      double foundRoot = left + (right - left) * 0.5;

      double functionValueAtRoot;

      if (!TryEvaluate(function, foundRoot, out functionValueAtRoot))
      {
        result.HasDiscontinuity = true;
        result.Error =
            "На интервале полюс, а не корень";
        return result;
      }

      result.Root = foundRoot;
      result.FunctionValueAtRoot = functionValueAtRoot;
      result.Iterations = iterationCount;

      // ===== 3. Проверка результата =====
      double scale =
          Math.Max(
              Math.Abs(leftValue),
              Math.Abs(functionValueAtRoot));

      double firstLeftValue;
      double firstRightValue;

      if (TryEvaluate(
              function,
              signChanges[0].l,
              out firstLeftValue)
          &&
          TryEvaluate(
              function,
              signChanges[0].r,
              out firstRightValue))
      {
        scale = Math.Max(
            Math.Abs(firstLeftValue),
            Math.Abs(firstRightValue));
      }

      if (double.IsNaN(scale)
          || double.IsInfinity(scale)
          || scale < 1e-12)
      {
        scale = 1.0;
      }

      double absoluteRootValue =
          Math.Abs(functionValueAtRoot);

      if (double.IsNaN(absoluteRootValue)
          || double.IsInfinity(absoluteRootValue)
          || absoluteRootValue > scale * 1e-3)
      {
        result.Success = false;
        result.HasDiscontinuity = true;
        result.Error =
            "На интервале найден разрыв (полюс), а не корень функции. f(x) не обращается в ноль.";
        return result;
      }

      result.Success = true;
      return result;
    }

    private static bool TryEvaluate(
        Func<double, double> function,
        double x,
        out double value)
    {
      try
      {
        value = function(x);

        if (double.IsNaN(value) || double.IsInfinity(value))
        {
          return false;
        }

        return true;
      }
      catch
      {
        value = double.NaN;
        return false;
      }
    }
  }
}