using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TimeLineTest;

/// <summary>
/// TimelineControl.xaml에 대한 상호 작용 논리
/// </summary>
public partial class TimelineControl : UserControl
{
  public TimeSpan Minimum { get; set; } = TimeSpan.FromMinutes(0);
  public TimeSpan Maximum { get; set; } = TimeSpan.FromMinutes(60);

  public TimeSpan CurrentTime
  {
    get => _currentTime;
    set
    {
      _currentTime = value;
      InvalidateVisual();
    }
  }

  private readonly int[] zoomSteps = { 1, 5, 10, 30, 60, 120, 240, 360, 480 }; // 분 단위
  private int zoomStepIndex = 2; // 기본값: 30분



  private DateTime startTime = new DateTime(2025, 1, 7, 13, 0, 0);
  private DateTime endTime = new DateTime(2025, 1, 7, 15, 0, 0);
  private TimeSpan zoomUnit = TimeSpan.FromMinutes(30); // 초기 줌 단위
  private bool isDraggingLeft;
  private Point mouseDownPosition;
  private bool wasDragged = false;
  private const double DragThreshold = 3; // 드래그로 판단할 거리 임계값
  private DateTime selectedTime = new DateTime(2025, 1, 7, 14, 0, 0);
  private TimeSpan _currentTime = TimeSpan.Zero;

  public List<(DateTime Start, DateTime End)> Recordings { get; set; } = new List<(DateTime, DateTime)>
              {
                  (new DateTime(2025, 1, 7, 14, 1, 0), new DateTime(2025, 1, 7, 14, 10, 0)),
                  (new DateTime(2025, 1, 7, 14, 30, 0), new DateTime(2025, 1, 7, 14, 35, 0)),
                  (new DateTime(2025, 1, 7, 14, 50, 0), new DateTime(2025, 1, 7, 14, 59, 0)),
              };
  public TimelineControl()
  {
    InitializeComponent();
    this.Loaded += (s, e) => Draw();
  }
  private void Draw()
  {
    PART_Canvas.Children.Clear();
    double width = PART_Canvas.ActualWidth;
    double height = PART_Canvas.ActualHeight;

    if (width == 0 || height == 0) return;

    TimeSpan totalSpan = endTime - startTime;

    // 기준 시간 텍스트  
    var baseTimeText = new TextBlock
    {
      Text = $"기준 시간: {startTime:yyyy-MM-dd HH:mm:ss}",
      Foreground = Brushes.White,
      FontSize = 12
    };
    Canvas.SetLeft(baseTimeText, 5);
    Canvas.SetTop(baseTimeText, 5);
    PART_Canvas.Children.Add(baseTimeText);

    // 선택된 시간 텍스트  
    var selectedTimeText = new TextBlock
    {
      Text = $"선택 시간: {selectedTime:HH:mm:ss}",
      Foreground = Brushes.Red,
      FontSize = 12
    };
    Canvas.SetLeft(selectedTimeText, 5);
    Canvas.SetTop(selectedTimeText, 25);
    PART_Canvas.Children.Add(selectedTimeText);

    // 가로 축 레이블 (시간)
    DateTime labelStart;
    TimeSpan labelStep;
    if (zoomUnit.TotalMinutes < 5)
    {
      // 5분 단위로 첫 레이블 위치 보정
      int startMinute = startTime.Minute + startTime.Hour * 60;
      labelStart = new DateTime(startTime.Year, startTime.Month, startTime.Day, 0, 0, 0);
      labelStep = TimeSpan.FromMinutes(1);
    }
    else if (zoomUnit.TotalMinutes < 10)
    {
      // 5분 단위로 첫 레이블 위치 보정
      int startMinute = startTime.Minute + startTime.Hour * 60;
      int firstLabelMinute = ((startMinute + 4) / 5) * 5;
      labelStart = new DateTime(startTime.Year, startTime.Month, startTime.Day, 0, 0, 0)
          .AddMinutes(firstLabelMinute);
      labelStep = TimeSpan.FromMinutes(5);
    }
    else if (zoomUnit.TotalMinutes < 30)
    {
      // 5분 단위로 첫 레이블 위치 보정
      int startMinute = startTime.Minute + startTime.Hour * 60;
      int firstLabelMinute = ((startMinute + 9) / 10) * 10;
      labelStart = new DateTime(startTime.Year, startTime.Month, startTime.Day, 0, 0, 0)
          .AddMinutes(firstLabelMinute);
      labelStep = TimeSpan.FromMinutes(10);
    }
    else if (zoomUnit.TotalMinutes < 60)
    {
      // 5분 단위로 첫 레이블 위치 보정
      int startMinute = startTime.Minute + startTime.Hour * 60;
      int firstLabelMinute = ((startMinute + 29) / 30) * 30;
      labelStart = new DateTime(startTime.Year, startTime.Month, startTime.Day, 0, 0, 0)
          .AddMinutes(firstLabelMinute);
      labelStep = TimeSpan.FromMinutes(30);
    }
    else
    {
      // 1시간 단위로 첫 레이블 위치 보정
      int startHour = startTime.Hour;
      labelStart = new DateTime(startTime.Year, startTime.Month, startTime.Day, startHour, 0, 0);
      if (labelStart < startTime)
        labelStart = labelStart.AddHours(1);
      labelStep = TimeSpan.FromHours(1);
    }

    int nCount = 0;
    // 가로 축 레이블 (시간)  
    for (DateTime t = labelStart; t <= endTime; t += labelStep)
    {
      double x = (t - startTime).TotalSeconds / totalSpan.TotalSeconds * width;

      // 줌 단위가 1시간(60분) 이상이면 1시간 단위로만 라벨 표시
      bool isLabeledTick = false;
      string labelText = t.ToString("HH:mm");
      bool isBigTick = false;
      if (labelStep == TimeSpan.FromMinutes(1))
      {
        x = (t - startTime).TotalSeconds / totalSpan.TotalSeconds * width;
        // 5분 단위로 라벨 표시
        if (nCount % 5 == 0)
        {
          labelText = t.ToString("mm");
          isBigTick = true;
        }
      }
      else if (labelStep == TimeSpan.FromMinutes(5))
      {
        x = (t - startTime).TotalSeconds / totalSpan.TotalSeconds * width;
        // 5분 단위로 라벨 표시
        //if (nCount % 5 == 0)
        {
          labelText = t.ToString("mm");
          isBigTick = true;
        }
      }
      else if (labelStep == TimeSpan.FromMinutes(10))
      {
        x = (t - startTime).TotalMinutes / totalSpan.TotalMinutes * width;
        // 10분 단위로 라벨 표시
        if (nCount % 2 == 0)
        {
          labelText = t.ToString("HH:mm");
          isBigTick = true;
        }
      }
      else if (labelStep == TimeSpan.FromMinutes(30))
      {
        x = (t - startTime).TotalMinutes / totalSpan.TotalMinutes * width;
        // 30분 단위로 라벨 표시
        //if (nCount % 30 == 0)
        {
          labelText = t.ToString("HH:mm");
          isBigTick = true;
        }
      }
      else if (labelStep >= TimeSpan.FromHours(1))
      {
        x = (t - startTime).TotalMinutes / totalSpan.TotalMinutes * width;
        // 60분 단위로 라벨 표시
        //if (nCount % 60 == 0)
        {
          labelText = t.ToString("HH:mm");
          isBigTick = true;
        }
      }
        // 수직 파란선
        var line1 = new Line
      {
        X1 = x,
        X2 = x,
        Stroke = Brushes.SteelBlue,
        StrokeThickness = 1
      };

      var line2 = new Line
      {
        X1 = x,
        X2 = x,
        Stroke = Brushes.SteelBlue,
        StrokeThickness = 1
      };

      if(isBigTick)
      {
        line1.Y1 = 20;
        line1.Y2 = 20 + 20; // 라벨 있는 곳은 전체

        line2.Y1 = height - 40;
        line2.Y2 = height - 20;

        isLabeledTick = true;
      }
      else
      {
        line1.Y1 = 20;
        line1.Y2 = 20 + 10; // 라벨 있는 곳은 전체

        line2.Y1 = height - 30;
        line2.Y2 = height - 20;
      }



      PART_Canvas.Children.Add(line1);
      PART_Canvas.Children.Add(line2);

      // 텍스트 라벨 추가
      if (isLabeledTick)
      {
        var label = new TextBlock
        {
          Text = labelText,
          Foreground = Brushes.LightGray,
          FontSize = 10
        };

        double labelWidth = 40;
        double labelLeft = Math.Max(0, Math.Min(x - labelWidth / 2, width - labelWidth));

        Canvas.SetLeft(label, labelLeft);
        Canvas.SetTop(label, height - 18);

        PART_Canvas.Children.Add(label);
      }

      nCount++;
    }

    // 녹화 영역 (가로 막대)  
    double barHeight = 10;
    double barY = height / 2 - barHeight / 2;

    foreach (var (recStart, recEnd) in Recordings)
    {
      if (recEnd < startTime || recStart > endTime) continue;

      double startX = ((recStart > startTime ? recStart : startTime) - startTime).TotalSeconds / totalSpan.TotalSeconds * width;
      double endX = ((recEnd < endTime ? recEnd : endTime) - startTime).TotalSeconds / totalSpan.TotalSeconds * width;

      var rect = new Rectangle
      {
        Width = Math.Max(endX - startX, 1),
        Height = barHeight,
        Fill = Brushes.DeepSkyBlue,
        RadiusX = 2,
        RadiusY = 2
      };
      Canvas.SetLeft(rect, startX);
      Canvas.SetTop(rect, barY);
      PART_Canvas.Children.Add(rect);
    }

    // 선택된 시간 라인 (빨간 수직선)
    double selX = (selectedTime - startTime).TotalSeconds / totalSpan.TotalSeconds * width;
    var redLine = new Line
    {
      X1 = selX,
      X2 = selX,
      Y1 = 20,
      Y2 = height - 20,
      Stroke = Brushes.Red,
      StrokeThickness = 2
    };
    PART_Canvas.Children.Add(redLine);
  }




  private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
  {
    // 마우스 위치 가져오기
    Point mousePos = e.GetPosition(PART_Canvas);
    double mouseRatio = mousePos.X / PART_Canvas.ActualWidth;
    TimeSpan totalSpan = endTime - startTime;
    DateTime mouseTime = startTime.AddSeconds(totalSpan.TotalSeconds * mouseRatio);


    int prevZoomStepIndex = zoomStepIndex;
    // 줌 단계 변경
    if (e.Delta > 0 && zoomStepIndex > 0)
      zoomStepIndex--;
    else if (e.Delta < 0 && zoomStepIndex < zoomSteps.Length - 1)
      zoomStepIndex++;
    // 변경이 없으면 return
    if (zoomStepIndex == prevZoomStepIndex)
      return;

    // 새로운 줌 단위 적용
    zoomUnit = TimeSpan.FromMinutes(zoomSteps[zoomStepIndex]);

    //// 새로운 totalSpan 계산 (기존 중심 시간 위치 변경)
    double newTotalMinutes = zoomSteps[zoomStepIndex];
    TimeSpan newTotalSpan = TimeSpan.FromMinutes(newTotalMinutes);

    // 마우스 위치의 시간(mouseTime)을 중심으로 startTime/endTime 조정
    startTime = mouseTime.AddMinutes(-newTotalSpan.TotalMinutes / 2);
    endTime = startTime.Add(newTotalSpan);
    Draw();

  }

  private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
  {
    mouseDownPosition = e.GetPosition(PART_Canvas);
    isDraggingLeft = true;
    wasDragged = false;
    PART_Canvas.CaptureMouse();
  }



  private void Canvas_MouseMove(object sender, MouseEventArgs e)
  {
    if (isDraggingLeft && e.LeftButton == MouseButtonState.Pressed)
    {
      Point current = e.GetPosition(PART_Canvas);
      double dx = current.X - mouseDownPosition.X;

      if (Math.Abs(dx) >= DragThreshold)
      {
        wasDragged = true;

        TimeSpan moveSpan = TimeSpan.FromSeconds((endTime - startTime).TotalSeconds * dx / PART_Canvas.ActualWidth);
        startTime = startTime.Add(-moveSpan);
        endTime = endTime.Add(-moveSpan);

        mouseDownPosition = current; // 계속 이동 반영
        Draw();
      }
    }
  }

  private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
  {
    if (e.ChangedButton == MouseButton.Left)
    {
      Point pos = e.GetPosition(PART_Canvas);
      double ratio = pos.X / PART_Canvas.ActualWidth;
      TimeSpan totalSpan = endTime - startTime;
      selectedTime = startTime.AddSeconds(totalSpan.TotalSeconds * ratio);
      Draw();
    }
  }

  private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
  {
    if (isDraggingLeft && !wasDragged)
    {
      Point clickPos = e.GetPosition(PART_Canvas);
      double ratio = clickPos.X / PART_Canvas.ActualWidth;
      TimeSpan totalSpan = endTime - startTime;

      selectedTime = startTime.AddSeconds(totalSpan.TotalSeconds * ratio);
    }

    isDraggingLeft = false;
    wasDragged = false;
    PART_Canvas.ReleaseMouseCapture();
    Draw();
  }
}
