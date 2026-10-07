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
        double a, double b, double eps,
        int signScanSegments = 400)
    {
      var result = new DichotomyResult();

      if (double.IsNaN(a) || double.IsNaN(b) || double.IsNaN(eps))
      { result.Error = "Входные значения не числа"; return result; }

      if (a >= b) { result.Error = "Требуется a < b"; return result; }
      if (eps <= 0) { result.Error = "Точность e должна быть > 0"; return result; }
      if (eps >= (b - a)) { result.Error = "Точность e больше длины интервала"; return result; }

      double fa, fb;
      if (!TryEvaluate(function, a, out fa))
      { result.Error = "f(a) не вычислима"; return result; }
      if (!TryEvaluate(function, b, out fb))
      { result.Error = "f(b) не вычислима"; return result; }

      // ===== Границы, попавшие точно в корень =====
      if (Math.Abs(fa) < 1e-15)
      {
        result.Root = a;
        result.FunctionValueAtRoot = fa;
        result.Success = true;
        result.Iterations = 0;
        result.FoundLeft = result.FoundRight = a;
        return result;
      }
      if (Math.Abs(fb) < 1e-15)
      {
        result.Root = b;
        result.FunctionValueAtRoot = fb;
        result.Success = true;
        result.Iterations = 0;
        result.FoundLeft = result.FoundRight = b;
        return result;
      }

      // ===== 1. Строгое сканирование смены знака =====
      // Корень добавляется в кандидаты ТОЛЬКО когда f(x_i) * f(x_{i+1}) < 0.
      // Никаких "|f| маленькое — считаем корнем" — это противоречит дихотомии.
      var candidates = new List<(double l, double r)>();
      double step = (b - a) / signScanSegments;
      double previousX = a;
      double previousValue = fa;
      bool hadDiscontinuity = false;

      for (int i = 1; i <= signScanSegments; ++i)
      {
        double currentX = (i == signScanSegments) ? b : a + i * step;
        double currentValue;

        if (!TryEvaluate(function, currentX, out currentValue))
        {
          hadDiscontinuity = true;
          previousX = currentX;
          previousValue = double.NaN;
          continue;
        }

        if (!double.IsNaN(previousValue) && previousValue * currentValue < 0)
        {
          candidates.Add((previousX, currentX));
        }

        previousX = currentX;
        previousValue = currentValue;
      }

      if (candidates.Count == 0)
      {
        result.Error = hadDiscontinuity
          ? "На интервале есть разрыв, но корень не обнаружен."
          : "Корень не обнаружен: функция не меняет знак на заданном интервале.";
        return result;
      }

      // ===== 2. Отсеиваем полюса =====
      var realRoots = new List<(double l, double r)>();
      foreach (var c in candidates)
      {
        double rl, rr, rEst;
        if (ProbeCandidate(function, c.l, c.r, out rl, out rr, out rEst))
          realRoots.Add((rl, rr));
        else
          hadDiscontinuity = true;
      }

      if (realRoots.Count == 0)
      {
        result.Error = "На интервале найден разрыв (полюс), а не корень функции. f(x) не обращается в ноль.";
        return result;
      }

      if (realRoots.Count > 1)
      {
        result.Error = $"У вас несколько корней на выбранном интервале ({realRoots.Count} шт.). Уточните [a,b].";
        return result;
      }

      // ===== 3. Уточнение корня бисекцией =====
      double left = realRoots[0].l;
      double right = realRoots[0].r;
      double leftValue;
      if (!TryEvaluate(function, left, out leftValue))
      { result.Error = "f(left) не вычислима"; return result; }

      int iter = 0;
      const int maxIter = 10000;

      while ((right - left) > eps && iter < maxIter)
      {
        ++iter;
        double mid = left + (right - left) * 0.5;
        double midValue;

        if (!TryEvaluate(function, mid, out midValue))
        {
          result.HasDiscontinuity = true;
          result.Error = "Функция не вычислима внутри интервала. Возможно, присутствует разрыв.";
          return result;
        }

        // Точное попадание в ноль — это законный выход (середина совпала с корнем).
        if (Math.Abs(midValue) < 1e-15)
        {
          left = right = mid;
          break;
        }

        if (leftValue * midValue < 0)
          right = mid;
        else
        {
          left = mid;
          leftValue = midValue;
        }
      }

      if (iter >= maxIter)
      { result.Error = "Превышено максимальное число итераций"; return result; }

      double root = left + (right - left) * 0.5;
      double fRoot;
      if (!TryEvaluate(function, root, out fRoot))
      {
        result.HasDiscontinuity = true;
        result.Error = "На интервале полюс, а не корень";
        return result;
      }

      result.Root = root;
      result.FunctionValueAtRoot = fRoot;
      result.Iterations = iter;
      result.FoundLeft = realRoots[0].l;
      result.FoundRight = realRoots[0].r;
      result.Success = true;
      return result;
    }

    /// <summary>
    /// Различает корень и полюс. Возвращает true, если это корень.
    /// Корнем считается ситуация, когда |f| в середине интервала
    /// становится МЕНЬШЕ, чем на исходных концах (для полюса — больше).
    /// </summary>
    private static bool ProbeCandidate(
        Func<double, double> f,
        double l, double r,
        out double refinedLeft, out double refinedRight,
        out double rootEstimate)
    {
      refinedLeft = l; refinedRight = r; rootEstimate = (l + r) * 0.5;

      double fl, fr;
      if (!TryEvaluate(f, l, out fl)) return false;
      if (!TryEvaluate(f, r, out fr)) return false;

      // Смена знака обязательна (мы её уже проверили при сканировании, но на всякий случай).
      if (Math.Sign(fl) == Math.Sign(fr)) return false;

      double left = l, right = r, leftValue = fl;
      double minAbsMid = double.MaxValue;
      const int probeIter = 60;

      for (int i = 0; i < probeIter; i++)
      {
        double mid = left + (right - left) * 0.5;
        double midVal;
        if (!TryEvaluate(f, mid, out midVal)) return false;

        double absMid = Math.Abs(midVal);
        if (absMid < minAbsMid) minAbsMid = absMid;

        if (leftValue * midVal < 0)
          right = mid;
        else
        {
          left = mid;
          leftValue = midVal;
        }

        if (right - left < 1e-15) break;
      }

      double minEnd = Math.Min(Math.Abs(fl), Math.Abs(fr));
      if (minEnd < 1e-15) minEnd = 1e-15;

      // Для полюса |f| в середине РАСТЁТ, для корня — ПАДАЕТ.
      if (minAbsMid > minEnd) return false;

      refinedLeft = left;
      refinedRight = right;
      rootEstimate = left + (right - left) * 0.5;
      return true;
    }

    private static bool TryEvaluate(Func<double, double> f, double x, out double v)
    {
      try
      {
        v = f(x);
        if (double.IsNaN(v) || double.IsInfinity(v)) return false;
        return true;
      }
      catch { v = double.NaN; return false; }
    }
  }
}