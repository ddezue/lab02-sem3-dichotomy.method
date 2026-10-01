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
    public static DichotomyResult Solve(Func<double, double> function, double a, double b, double eps,
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

      // ===== 1. Сканирование: смена знака и разрывы =====
      var signChanges = new List<(double l, double r)>();
      double step = (b - a) / signScanSegments;

      double previousX = a;
      double previousValue;
      if (!TryEvaluate(function, a, out previousValue))
      {
        result.Error = "f(a) не вычислима";
        return result;
      }

      for (int scanIndex = 1; scanIndex <= signScanSegments; ++scanIndex)
      {
        double currentX = (scanIndex == signScanSegments) ? b : a + scanIndex * step;
        double currentValue;

        // при ошибке НЕ сбрасываем previousValue — иначе потеряем смену знака через полюс
        if (!TryEvaluate(function, currentX, out currentValue)
            || double.IsNaN(currentValue) || double.IsInfinity(currentValue))
        {
          result.HasDiscontinuity = true;
          continue;
        }

        if (!double.IsNaN(previousValue) && !double.IsInfinity(previousValue))
        {
          if (previousValue == 0)
            signChanges.Add((previousX, previousX));
          else if (currentValue == 0)
            signChanges.Add((currentX, currentX));
          else if (previousValue * currentValue < 0)
            signChanges.Add((previousX, currentX));
        }

        previousX = currentX;
        previousValue = currentValue;
      }

      if (signChanges.Count == 0)
      {
        result.Error = result.HasDiscontinuity
            ? "На интервале есть разрыв, но корня не обнаружено"
            : "На заданном интервале корень не обнаружен (нет смены знака)";
        return result;
      }
      if (signChanges.Count > 1)
      {
        result.Error = $"У вас несколько корней на выбранном интервале ({signChanges.Count} шт.). Уточните [a,b].";
        return result;
      }

      double left = signChanges[0].l;
      double right = signChanges[0].r;
      result.FoundLeft = left;
      result.FoundRight = right;

      if (Math.Abs(right - left) < 1e-15)
      {
        result.Root = left;
        if (!TryEvaluate(function, left, out result.FunctionValueAtRoot))
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
        return result;
      }

      double leftValue;
      if (!TryEvaluate(function, left, out leftValue))
      {
        result.Error = "f(left) не вычислима";
        return result;
      }

      // ===== 2. Бисекция =====
      int iterationCount = 0;
      const int maxIterations = 10000;

      while ((right - left) > eps && iterationCount < maxIterations)
      {
        ++iterationCount;
        double middle = left + (right - left) * 0.5;

        double middleValue;
        if (!TryEvaluate(function, middle, out middleValue)
            || double.IsNaN(middleValue) || double.IsInfinity(middleValue))
        {
          result.HasDiscontinuity = true;
          right = middle;
          if (right - left <= 0)
          {
            result.Error = "Функция не вычислима на интервале";
            return result;
          }
          continue;
        }

        if (Math.Abs(middleValue) < 1e-15)
        {
          left = right = middle;
          break;
        }

        if (leftValue * middleValue < 0)
          right = middle;
        else
        {
          left = middle;
          leftValue = middleValue;
        }
      }

      if (iterationCount >= maxIterations)
      {
        result.Error = "Превышено максимальное число итераций";
        return result;
      }

      double foundRoot = left + (right - left) * 0.5;
      double functionValueAtRoot;
      if (!TryEvaluate(function, foundRoot, out functionValueAtRoot))
      {
        result.HasDiscontinuity = true;
        result.Error = "На интервале полюс, а не корень";
        return result;
      }

      result.Root = foundRoot;
      result.FunctionValueAtRoot = functionValueAtRoot;
      result.Iterations = iterationCount;

      // ===== 3. Валидация: корень или полюс? =====
      double scale = Math.Max(Math.Abs(leftValue), Math.Abs(functionValueAtRoot));
      double firstLeftValue, firstRightValue;
      if (TryEvaluate(function, signChanges[0].l, out firstLeftValue)
          && TryEvaluate(function, signChanges[0].r, out firstRightValue))
      {
        scale = Math.Max(Math.Abs(firstLeftValue), Math.Abs(firstRightValue));
      }

      if (double.IsNaN(scale) || double.IsInfinity(scale) || scale < 1e-12)
        scale = 1.0;

      double absoluteRootValue = Math.Abs(functionValueAtRoot);
      if (double.IsNaN(absoluteRootValue) || double.IsInfinity(absoluteRootValue)
          || absoluteRootValue > scale * 1e-3)
      {
        result.Success = false;
        result.HasDiscontinuity = true;
        result.Error = "На интервале найден разрыв (полюс), а не корень функции. f(x) не обращается в ноль.";
        return result;
      }

      result.Success = true;
      return result;
    }

    private static bool TryEvaluate(Func<double, double> function, double x, out double value)
    {
      try
      {
        value = function(x);
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