using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    //StartTime, EndTime 사이의 단위
    private readonly int[] zoomSteps = { 5, 10, 60, 60*2, 60 * 4, 60 * 10, 60 * 24}; // 분 단위

    private int zoomStepIndex = 2; // 기본값: 30분

    //기준 눈금 사이의 간격
    private bool isDraggingLeft;
    private Point mouseDownPosition;
    private bool wasDragged = false;
    private const double DragThreshold = 3; // 드래그로 판단할 거리 임계값

    public static readonly DependencyProperty StartTimeProperty =
            DependencyProperty.Register(nameof(StartTime), typeof(DateTime), typeof(TimelineControl),
                new PropertyMetadata(new DateTime(2025, 1, 7, 13, 0, 0), OnAnyPropertyChanged));

    public static readonly DependencyProperty EndTimeProperty =
        DependencyProperty.Register(nameof(EndTime), typeof(DateTime), typeof(TimelineControl),
            new PropertyMetadata(new DateTime(2025, 1, 7, 15, 0, 0), OnAnyPropertyChanged));

    public static readonly DependencyProperty SelectedTimeProperty =
        DependencyProperty.Register(nameof(SelectedTime), typeof(DateTime), typeof(TimelineControl),
            new PropertyMetadata(new DateTime(2025, 1, 7, 14, 0, 0), OnAnyPropertyChanged));

    public static readonly DependencyProperty RecordingsProperty =
        DependencyProperty.Register(nameof(Recordings), typeof(ObservableCollection<(DateTime Start, DateTime End)>), typeof(TimelineControl),
            new PropertyMetadata(new ObservableCollection<(DateTime, DateTime)>(), OnAnyPropertyChanged));

    public DateTime StartTime
    {
        get => (DateTime)GetValue(StartTimeProperty);
        set => SetValue(StartTimeProperty, value);
    }

    public DateTime EndTime
    {
        get => (DateTime)GetValue(EndTimeProperty);
        set => SetValue(EndTimeProperty, value);
    }

    public DateTime SelectedTime
    {
        get => (DateTime)GetValue(SelectedTimeProperty);
        set => SetValue(SelectedTimeProperty, value);
    }

    public ObservableCollection<(DateTime Start, DateTime End)> Recordings
    {
        get => (ObservableCollection<(DateTime Start, DateTime End)>)GetValue(RecordingsProperty);
        set => SetValue(RecordingsProperty, value);
    }

    public TimelineControl()
    {
        InitializeComponent();
        this.Loaded += (s, e) => Draw();
    }


    private static void OnAnyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimelineControl control)
            control.Draw();
    }

    private void Draw()
    {
        //if (PART_Canvas == null) return;

        PART_Canvas.Children.Clear();
        double width = PART_Canvas.ActualWidth;
        double height = PART_Canvas.ActualHeight;

        if (width == 0 || height == 0) return;

        TimeSpan totalSpan = EndTime - StartTime;

        // 기준 시간 텍스트  
        var baseTimeText = new TextBlock
        {
            Text = $"기준 시간: {StartTime:yyyy-MM-dd HH:mm:ss}",
            Foreground = Brushes.Black,
            FontSize = 12,
        };
        Canvas.SetLeft(baseTimeText, 5);
        Canvas.SetTop(baseTimeText, 5);
        PART_Canvas.Children.Add(baseTimeText);

        // 선택된 시간 텍스트  
        var SelectedTimeText = new TextBlock
        {
            Text = $"선택 시간: {SelectedTime:HH:mm:ss}",
            Foreground = Brushes.Red,
            FontSize = 12,
        };
        Canvas.SetLeft(SelectedTimeText, 5);
        Canvas.SetTop(SelectedTimeText, 25);
        PART_Canvas.Children.Add(SelectedTimeText);

        // 가로 축 레이블 (시간)

        double totalMinutes = totalSpan.TotalMinutes;
        double totalHours = totalSpan.TotalHours;


        DateTime labelStart;
        TimeSpan labelStep;
        int BigTickEvery = 0;
        string labelFormat = "HH:mm";
        int startMinute = StartTime.Minute + StartTime.Hour * 60;
        int zoomStepIndexTemp = 0;
        if (totalMinutes <= zoomSteps[zoomStepIndexTemp++])
        {
            // 총 시간 5분, 5초 간격, 1분(12개)마다 메이저
            labelStart = new DateTime(StartTime.Year, StartTime.Month, StartTime.Day, StartTime.Hour, StartTime.Minute, 0);
            labelStep = TimeSpan.FromSeconds(5);
            BigTickEvery = 12;
            labelFormat = "HH:mm:ss";
        }
        else if (totalMinutes <= zoomSteps[zoomStepIndexTemp++])
        {
            // 총 시간 10분, 10초 간격, 1분(6개)마다 메이저
            int firstLabelMinute = (startMinute / 5) * 5;
            labelStart = new DateTime(StartTime.Year, StartTime.Month, StartTime.Day, 0, 0, 0)
          .AddMinutes(firstLabelMinute);
            labelStep = TimeSpan.FromSeconds(10);
            BigTickEvery = 6;
            labelFormat = "HH:mm";
        }
        else if (totalMinutes <= zoomSteps[zoomStepIndexTemp++])
        {
            // 총 시간 60분, 1분 간격, 10분(10개)마다 메이저
            int firstLabelMinute = ((startMinute) / 10) * 10;
            labelStart = new DateTime(StartTime.Year, StartTime.Month, StartTime.Day, 0, 0, 0)
                .AddMinutes(firstLabelMinute);
            labelStep = TimeSpan.FromMinutes(1);
            BigTickEvery = 10;
            labelFormat = "HH:mm";
        }
        else if (totalMinutes <= zoomSteps[zoomStepIndexTemp++])
        {
            // 총 시간 2시간, 5분 간격, 10분(2개)마다 메이저
            int firstLabelMinute = ((startMinute) / 10) * 10;
            labelStart = new DateTime(StartTime.Year, StartTime.Month, StartTime.Day, 0, 0, 0)
                .AddMinutes(firstLabelMinute);
            labelStep = TimeSpan.FromMinutes(5);
            BigTickEvery = 2;
            labelFormat = "HH::mm";
        }
        else if (totalMinutes <= zoomSteps[zoomStepIndexTemp++])
        {
            // 총 시간 4시간, 10분 간격, 30분(3개)마다 메이저
            int firstLabelMinute = ((startMinute) / 30) * 30;
            labelStart = new DateTime(StartTime.Year, StartTime.Month, StartTime.Day, 0, 0, 0)
                .AddMinutes(firstLabelMinute);
            labelStep = TimeSpan.FromMinutes(10);
            BigTickEvery = 3;
            labelFormat = "HH:mm";
        }
        else if (totalMinutes <= zoomSteps[zoomStepIndexTemp++])
        {

            // 총 시간 10시간, 30분 간격, 30분(2개)마다 메이저
            int firstLabelMinute = ((startMinute) / 60) * 60;
            labelStart = new DateTime(StartTime.Year, StartTime.Month, StartTime.Day, 0, 0, 0)
                .AddMinutes(firstLabelMinute);
            labelStep = TimeSpan.FromMinutes(30);
            BigTickEvery = 2;
            labelFormat = "HH:mm";
        }
        else if (totalMinutes <= zoomSteps[zoomStepIndexTemp++])
        {

            // 총 시간 24시간, 1시간 간격, 60분(1개)마다 메이저
            int firstLabelMinute = ((startMinute) / 60) * 60;
            labelStart = new DateTime(StartTime.Year, StartTime.Month, StartTime.Day, 0, 0, 0)
                .AddMinutes(firstLabelMinute);
            labelStep = TimeSpan.FromMinutes(60);
            BigTickEvery = 1;
            labelFormat = "HH";
        }
        else
        {
            // 60분 간격, 매번 메이저
            int startHour = StartTime.Hour;
            labelStart = new DateTime(StartTime.Year, StartTime.Month, StartTime.Day, startHour, 0, 0);
            if (labelStart < StartTime)
                labelStart = labelStart.AddHours(1);
            labelStep = TimeSpan.FromHours(1);
            BigTickEvery = 3;
            labelFormat = "HH";
        }

        int nCount = 0;
        // 가로 축 레이블 (시간)  
        for (DateTime t = labelStart; t <= EndTime; t += labelStep)
        {
            double x = (t - StartTime).TotalSeconds / totalSpan.TotalSeconds * width;

            // 줌 단위가 1시간(60분) 이상이면 1시간 단위로만 라벨 표시
            bool isLabeledTick = false;
            string labelText = t.ToString(labelFormat);
            bool isBigTick = false;

            if(nCount % BigTickEvery == 0)
            {
                isBigTick = true;
            }
           

            // 수직 파란선
            var line1 = new Line
            {
                X1 = x,
                X2 = x,
                Stroke = Brushes.SteelBlue,
                StrokeThickness = 1,
            };

            var line2 = new Line
            {
                X1 = x,
                X2 = x,
                Stroke = Brushes.SteelBlue,
                StrokeThickness = 1,
            };

            if (isBigTick)
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
                    Foreground = Brushes.Black,
                    FontSize = 10,
                };

                double labelWidth = label.Text.Length * 5;
                double labelLeft =Math.Min(x - labelWidth / 2, width - labelWidth);

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
            if (recEnd < StartTime || recStart > EndTime) continue;

            double startX = ((recStart > StartTime ? recStart : StartTime) - StartTime).TotalSeconds / totalSpan.TotalSeconds * width;
            double endX = ((recEnd < EndTime ? recEnd : EndTime) - StartTime).TotalSeconds / totalSpan.TotalSeconds * width;

            var rect = new Rectangle
            {
                Width = Math.Max(endX - startX, 1),
                Height = barHeight,
                Fill = Brushes.DeepSkyBlue,
                RadiusX = 2,
                RadiusY = 2,
            };
            Canvas.SetLeft(rect, startX);
            Canvas.SetTop(rect, barY);
            PART_Canvas.Children.Add(rect);
        }

        // 선택된 시간 라인 (빨간 수직선)
        double selX = (SelectedTime - StartTime).TotalSeconds / totalSpan.TotalSeconds * width;
        var redLine = new Line
        {
            X1 = selX,
            X2 = selX,
            Y1 = 20,
            Y2 = height - 20,
            Stroke = Brushes.Red,
            StrokeThickness = 2,
        };
        PART_Canvas.Children.Add(redLine);
    }




    private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        // 마우스 위치 가져오기
        Point mousePos = e.GetPosition(PART_Canvas);
        double mouseRatio = mousePos.X / PART_Canvas.ActualWidth;
        TimeSpan totalSpan = EndTime - StartTime;
        DateTime mouseTime = StartTime.AddSeconds(totalSpan.TotalSeconds * mouseRatio);


        int prevZoomStepIndex = zoomStepIndex;
        // 줌 단계 변경
        if (e.Delta > 0 && zoomStepIndex > 0)
            zoomStepIndex--;
        else if (e.Delta < 0 && zoomStepIndex < zoomSteps.Length - 1)
            zoomStepIndex++;
        // 변경이 없으면 return
        if (zoomStepIndex == prevZoomStepIndex)
            return;

        //// 새로운 totalSpan 계산 (기존 중심 시간 위치 변경)
        double newTotalMinutes = zoomSteps[zoomStepIndex];
        TimeSpan newTotalSpan = TimeSpan.FromMinutes(newTotalMinutes);

        // 마우스 위치의 시간(mouseTime)을 중심으로 StartTime/EndTime 조정
        StartTime = mouseTime.AddMinutes(-newTotalSpan.TotalMinutes / 2);
        EndTime = StartTime.Add(newTotalSpan);
        Draw();

        Trace.WriteLine($"Zoomed to {newTotalSpan.TotalMinutes} minutes. Start: {StartTime}, End: {EndTime}, Mouse Time: {mouseTime}");
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

                TimeSpan moveSpan = TimeSpan.FromSeconds((EndTime - StartTime).TotalSeconds * dx / PART_Canvas.ActualWidth);
                StartTime = StartTime.Add(-moveSpan);
                EndTime = EndTime.Add(-moveSpan);

                mouseDownPosition = current; // 계속 이동 반영
                Draw();
            }
        }
    }

    private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (PART_Canvas == null) return;
        if (e.ChangedButton == MouseButton.Left)
        {
            Point pos = e.GetPosition(PART_Canvas);
            double ratio = pos.X / PART_Canvas.ActualWidth;
            TimeSpan totalSpan = EndTime - StartTime;
            SelectedTime = StartTime.AddSeconds(totalSpan.TotalSeconds * ratio);
            Draw();
        }
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (isDraggingLeft && !wasDragged)
        {
            Point clickPos = e.GetPosition(PART_Canvas);
            double ratio = clickPos.X / PART_Canvas.ActualWidth;
            TimeSpan totalSpan = EndTime - StartTime;

            SelectedTime = StartTime.AddSeconds(totalSpan.TotalSeconds * ratio);
        }

        isDraggingLeft = false;
        wasDragged = false;
        PART_Canvas.ReleaseMouseCapture();
        Draw();
    }
}
