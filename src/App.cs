// ============================================================
//  QUANTUM SHUTDOWN · 智能关机控制台 v2.1.1
//  纯 C# WPF 单文件实现，csc.exe 直接编译为独立 exe
//  功能：倒计时关机 / 定时关机 / 一键取消（shutdown /a）
//  自测：智能关机控制台.exe --selftest  /  --shots（附截图）
// ============================================================
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace QuantumShutdown
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            string mode = args != null && args.Length > 0 ? args[0] : "";
            var win = (Window)XamlReader.Parse(Assets.MainXaml);
            var ctl = new Controller(win);
            if (mode == "--selftest" || mode == "--shots")
            {
                ctl.RunSelfTest(mode == "--shots");
                return;
            }
            var app = new Application();
            app.Run(win);
        }
    }

    // ============================ XAML ============================
    internal static class Assets
    {
        public const string MainXaml = @"
<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
        Title=""智能关机控制台"" Width=""880"" Height=""600""
        WindowStartupLocation=""CenterScreen"" WindowStyle=""None""
        AllowsTransparency=""True"" Background=""Transparent""
        ResizeMode=""NoResize"" FontFamily=""Microsoft YaHei UI""
        UseLayoutRounding=""True"" TextOptions.TextFormattingMode=""Display"">
  <Window.Resources>

    <LinearGradientBrush x:Key=""PrimGrad"" StartPoint=""0,0"" EndPoint=""1,1"">
      <GradientStop Color=""#2ADAF7"" Offset=""0""/>
      <GradientStop Color=""#2F6BFF"" Offset=""1""/>
    </LinearGradientBrush>

    <LinearGradientBrush x:Key=""WinGrad"" StartPoint=""0,0"" EndPoint=""1,1"">
      <GradientStop Color=""#0D1A38"" Offset=""0""/>
      <GradientStop Color=""#0A1226"" Offset=""0.55""/>
      <GradientStop Color=""#060A16"" Offset=""1""/>
    </LinearGradientBrush>

    <DrawingBrush x:Key=""GridBrush"" TileMode=""Tile"" ViewportUnits=""Absolute"" Viewport=""0,0,44,44"">
      <DrawingBrush.Drawing>
        <GeometryDrawing Geometry=""M0,0 L44,0 M0,0 L0,44"">
          <GeometryDrawing.Pen>
            <Pen Thickness=""1"">
              <Pen.Brush><SolidColorBrush Color=""#7FB3FF"" Opacity=""0.055""/></Pen.Brush>
            </Pen>
          </GeometryDrawing.Pen>
        </GeometryDrawing>
      </DrawingBrush.Drawing>
    </DrawingBrush>

    <Style x:Key=""Lbl"" TargetType=""TextBlock"">
      <Setter Property=""FontSize"" Value=""10.5""/>
      <Setter Property=""FontWeight"" Value=""SemiBold""/>
      <Setter Property=""Foreground"" Value=""#98ADD3""/>
      <Setter Property=""Margin"" Value=""0,0,0,8""/>
    </Style>

    <Style x:Key=""ChipBtn"" TargetType=""Button"">
      <Setter Property=""Foreground"" Value=""#CBD9F2""/>
      <Setter Property=""FontSize"" Value=""12""/>
      <Setter Property=""Margin"" Value=""0,0,8,8""/>
      <Setter Property=""Cursor"" Value=""Hand""/>
      <Setter Property=""Template"">
        <Setter.Value>
          <ControlTemplate TargetType=""Button"">
            <Border x:Name=""bd"" CornerRadius=""8"" Background=""#0C1729"" BorderBrush=""#24406E"" BorderThickness=""1"" Padding=""14,6"">
              <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
            </Border>
            <ControlTemplate.Triggers>
              <Trigger Property=""IsMouseOver"" Value=""True"">
                <Setter TargetName=""bd"" Property=""BorderBrush"" Value=""#2BE4FF""/>
                <Setter TargetName=""bd"" Property=""Background"" Value=""#12233F""/>
                <Setter Property=""Foreground"" Value=""#2BE4FF""/>
              </Trigger>
              <Trigger Property=""IsPressed"" Value=""True"">
                <Setter TargetName=""bd"" Property=""Opacity"" Value=""0.8""/>
              </Trigger>
            </ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>

    <Style x:Key=""PrimaryBtn"" TargetType=""Button"">
      <Setter Property=""Height"" Value=""46""/>
      <Setter Property=""FontSize"" Value=""14""/>
      <Setter Property=""FontWeight"" Value=""Bold""/>
      <Setter Property=""Foreground"" Value=""#04121F""/>
      <Setter Property=""Cursor"" Value=""Hand""/>
      <Setter Property=""Template"">
        <Setter.Value>
          <ControlTemplate TargetType=""Button"">
            <Border x:Name=""bd"" CornerRadius=""10"" Background=""{StaticResource PrimGrad}"">
              <Border.Effect>
                <DropShadowEffect Color=""#1FB6FF"" BlurRadius=""18"" ShadowDepth=""0"" Opacity=""0.45""/>
              </Border.Effect>
              <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
            </Border>
            <ControlTemplate.Triggers>
              <Trigger Property=""IsMouseOver"" Value=""True"">
                <Setter TargetName=""bd"" Property=""Effect"">
                  <Setter.Value>
                    <DropShadowEffect Color=""#2BE4FF"" BlurRadius=""28"" ShadowDepth=""0"" Opacity=""0.75""/>
                  </Setter.Value>
                </Setter>
              </Trigger>
              <Trigger Property=""IsPressed"" Value=""True"">
                <Setter TargetName=""bd"" Property=""Opacity"" Value=""0.85""/>
              </Trigger>
              <Trigger Property=""IsEnabled"" Value=""False"">
                <Setter TargetName=""bd"" Property=""Opacity"" Value=""0.35""/>
              </Trigger>
            </ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>

    <Style x:Key=""GhostBtn"" TargetType=""Button"">
      <Setter Property=""Height"" Value=""38""/>
      <Setter Property=""FontSize"" Value=""12.5""/>
      <Setter Property=""Foreground"" Value=""#FF8AA0""/>
      <Setter Property=""Cursor"" Value=""Hand""/>
      <Setter Property=""Template"">
        <Setter.Value>
          <ControlTemplate TargetType=""Button"">
            <Border x:Name=""bd"" CornerRadius=""10"" Background=""#170F1E"" BorderBrush=""#4A2A3C"" BorderThickness=""1"">
              <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
            </Border>
            <ControlTemplate.Triggers>
              <Trigger Property=""IsMouseOver"" Value=""True"">
                <Setter TargetName=""bd"" Property=""BorderBrush"" Value=""#FF5C7A""/>
                <Setter TargetName=""bd"" Property=""Background"" Value=""#221426""/>
                <Setter Property=""Foreground"" Value=""#FFB3C2""/>
              </Trigger>
              <Trigger Property=""IsPressed"" Value=""True"">
                <Setter TargetName=""bd"" Property=""Opacity"" Value=""0.8""/>
              </Trigger>
              <Trigger Property=""IsEnabled"" Value=""False"">
                <Setter TargetName=""bd"" Property=""Opacity"" Value=""0.35""/>
              </Trigger>
            </ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>

    <Style x:Key=""AbortBtn"" TargetType=""Button"">
      <Setter Property=""Height"" Value=""30""/>
      <Setter Property=""FontSize"" Value=""11""/>
      <Setter Property=""Foreground"" Value=""#7FDFFF""/>
      <Setter Property=""Cursor"" Value=""Hand""/>
      <Setter Property=""Template"">
        <Setter.Value>
          <ControlTemplate TargetType=""Button"">
            <Border x:Name=""bd"" CornerRadius=""8"" Background=""#0C1B2E"" BorderBrush=""#24507A"" BorderThickness=""1"" Padding=""13,4"">
              <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
            </Border>
            <ControlTemplate.Triggers>
              <Trigger Property=""IsMouseOver"" Value=""True"">
                <Setter TargetName=""bd"" Property=""BorderBrush"" Value=""#2BE4FF""/>
                <Setter Property=""Foreground"" Value=""#2BE4FF""/>
              </Trigger>
              <Trigger Property=""IsEnabled"" Value=""False"">
                <Setter TargetName=""bd"" Property=""Opacity"" Value=""0.35""/>
              </Trigger>
            </ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>

    <Style x:Key=""TabBtn"" TargetType=""RadioButton"">
      <Setter Property=""Foreground"" Value=""#B0C2E4""/>
      <Setter Property=""FontSize"" Value=""13""/>
      <Setter Property=""FontWeight"" Value=""SemiBold""/>
      <Setter Property=""Cursor"" Value=""Hand""/>
      <Setter Property=""Template"">
        <Setter.Value>
          <ControlTemplate TargetType=""RadioButton"">
            <Border x:Name=""bd"" CornerRadius=""8"" Background=""Transparent"" BorderThickness=""1"" BorderBrush=""Transparent"">
              <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
            </Border>
            <ControlTemplate.Triggers>
              <Trigger Property=""IsMouseOver"" Value=""True"">
                <Setter TargetName=""bd"" Property=""BorderBrush"" Value=""#26436E""/>
              </Trigger>
              <Trigger Property=""IsChecked"" Value=""True"">
                <Setter TargetName=""bd"" Property=""Background"" Value=""{StaticResource PrimGrad}""/>
                <Setter TargetName=""bd"" Property=""Effect"">
                  <Setter.Value>
                    <DropShadowEffect Color=""#1FB6FF"" BlurRadius=""14"" ShadowDepth=""0"" Opacity=""0.4""/>
                  </Setter.Value>
                </Setter>
                <Setter Property=""Foreground"" Value=""#04121F""/>
              </Trigger>
            </ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>

    <Style x:Key=""StepBox"" TargetType=""TextBox"">
      <Setter Property=""Background"" Value=""Transparent""/>
      <Setter Property=""Foreground"" Value=""#EAF4FF""/>
      <Setter Property=""CaretBrush"" Value=""#2BE4FF""/>
      <Setter Property=""BorderThickness"" Value=""0""/>
      <Setter Property=""FontFamily"" Value=""Cascadia Mono, Consolas""/>
      <Setter Property=""FontSize"" Value=""17""/>
      <Setter Property=""TextAlignment"" Value=""Center""/>
      <Setter Property=""VerticalContentAlignment"" Value=""Center""/>
      <Setter Property=""Template"">
        <Setter.Value>
          <ControlTemplate TargetType=""TextBox"">
            <Border Background=""Transparent"">
              <ScrollViewer x:Name=""PART_ContentHost"" VerticalAlignment=""Center""/>
            </Border>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>

    <Style x:Key=""IconBtn"" TargetType=""Button"">
      <Setter Property=""Width"" Value=""34""/>
      <Setter Property=""Height"" Value=""26""/>
      <Setter Property=""FontFamily"" Value=""Segoe MDL2 Assets""/>
      <Setter Property=""FontSize"" Value=""10.5""/>
      <Setter Property=""Foreground"" Value=""#B0C2E4""/>
      <Setter Property=""Cursor"" Value=""Hand""/>
      <Setter Property=""Template"">
        <Setter.Value>
          <ControlTemplate TargetType=""Button"">
            <Border x:Name=""bd"" CornerRadius=""5"" Background=""Transparent"">
              <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
            </Border>
            <ControlTemplate.Triggers>
              <Trigger Property=""IsMouseOver"" Value=""True"">
                <Setter TargetName=""bd"" Property=""Background"" Value=""#182A47""/>
                <Setter Property=""Foreground"" Value=""#D7E6FF""/>
              </Trigger>
            </ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>

    <Style x:Key=""CloseBtn"" TargetType=""Button"">
      <Setter Property=""Width"" Value=""34""/>
      <Setter Property=""Height"" Value=""26""/>
      <Setter Property=""FontFamily"" Value=""Segoe MDL2 Assets""/>
      <Setter Property=""FontSize"" Value=""10.5""/>
      <Setter Property=""Foreground"" Value=""#B0C2E4""/>
      <Setter Property=""Cursor"" Value=""Hand""/>
      <Setter Property=""Template"">
        <Setter.Value>
          <ControlTemplate TargetType=""Button"">
            <Border x:Name=""bd"" CornerRadius=""5"" Background=""Transparent"">
              <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
            </Border>
            <ControlTemplate.Triggers>
              <Trigger Property=""IsMouseOver"" Value=""True"">
                <Setter TargetName=""bd"" Property=""Background"" Value=""#C42B40""/>
                <Setter Property=""Foreground"" Value=""#FFFFFF""/>
              </Trigger>
            </ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>

  </Window.Resources>

  <Window.Triggers>
    <EventTrigger RoutedEvent=""Window.Loaded"">
      <BeginStoryboard>
        <Storyboard RepeatBehavior=""Forever"">
          <DoubleAnimation Storyboard.TargetName=""ScanTf"" Storyboard.TargetProperty=""Y""
                           From=""-150"" To=""640"" Duration=""0:0:9""/>
        </Storyboard>
      </BeginStoryboard>
    </EventTrigger>
  </Window.Triggers>

  <Grid Margin=""22"">
    <Border CornerRadius=""16"" BorderThickness=""1"" BorderBrush=""#223A66"" Background=""{StaticResource WinGrad}"">
      <Border.Effect>
        <DropShadowEffect Color=""#1E9BFF"" BlurRadius=""34"" ShadowDepth=""0"" Opacity=""0.32""/>
      </Border.Effect>
      <Grid>
        <Grid.RowDefinitions>
          <RowDefinition Height=""Auto""/>
          <RowDefinition Height=""*""/>
        </Grid.RowDefinitions>

        <Rectangle Grid.RowSpan=""2"" RadiusX=""16"" RadiusY=""16"" Fill=""{StaticResource GridBrush}"" IsHitTestVisible=""False""/>

        <Ellipse Grid.RowSpan=""2"" Width=""560"" Height=""560"" HorizontalAlignment=""Right"" VerticalAlignment=""Top""
                 Margin=""0,-260,-160,0"" Opacity=""0.5"" IsHitTestVisible=""False"">
          <Ellipse.Fill>
            <RadialGradientBrush>
              <GradientStop Color=""#552BE4FF"" Offset=""0""/>
              <GradientStop Color=""#002BE4FF"" Offset=""1""/>
            </RadialGradientBrush>
          </Ellipse.Fill>
        </Ellipse>

        <Ellipse Grid.RowSpan=""2"" Width=""520"" Height=""520"" HorizontalAlignment=""Left"" VerticalAlignment=""Bottom""
                 Margin=""-180,0,0,-240"" Opacity=""0.45"" IsHitTestVisible=""False"">
          <Ellipse.Fill>
            <RadialGradientBrush>
              <GradientStop Color=""#403E8BFF"" Offset=""0""/>
              <GradientStop Color=""#003E8BFF"" Offset=""1""/>
            </RadialGradientBrush>
          </Ellipse.Fill>
        </Ellipse>

        <Rectangle Grid.RowSpan=""2"" x:Name=""ScanRect"" Height=""130"" VerticalAlignment=""Top"" Opacity=""0.05"" IsHitTestVisible=""False"">
          <Rectangle.Fill>
            <LinearGradientBrush StartPoint=""0,0"" EndPoint=""0,1"">
              <GradientStop Color=""#002BE4FF"" Offset=""0""/>
              <GradientStop Color=""#FF2BE4FF"" Offset=""0.5""/>
              <GradientStop Color=""#002BE4FF"" Offset=""1""/>
            </LinearGradientBrush>
          </Rectangle.Fill>
          <Rectangle.RenderTransform>
            <TranslateTransform x:Name=""ScanTf"" Y=""-150""/>
          </Rectangle.RenderTransform>
        </Rectangle>

        <!-- ==================== 标题栏 ==================== -->
        <Border x:Name=""TitleBar"" Grid.Row=""0"" Height=""46"" Background=""Transparent"">
          <DockPanel Margin=""20,0,12,0"" LastChildFill=""True"">
            <StackPanel DockPanel.Dock=""Right"" Orientation=""Horizontal"" VerticalAlignment=""Center"">
              <Button x:Name=""BtnMin"" Style=""{StaticResource IconBtn}"" Content=""&#xE921;""/>
              <Button x:Name=""BtnClose"" Style=""{StaticResource CloseBtn}"" Content=""&#xE8BB;""/>
            </StackPanel>
            <StackPanel DockPanel.Dock=""Left"" Orientation=""Horizontal"" VerticalAlignment=""Center"">
              <Grid Width=""22"" Height=""22"">
                <Ellipse Fill=""#0E2440"" Stroke=""#2BE4FF"" StrokeThickness=""1"" Opacity=""0.9""/>
                <TextBlock Text=""&#xE7E8;"" FontFamily=""Segoe MDL2 Assets"" FontSize=""11""
                           Foreground=""#2BE4FF"" HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
              </Grid>
              <TextBlock Margin=""10,0,0,0"" Text=""QUANTUM SHUTDOWN"" FontSize=""11.5"" FontWeight=""SemiBold""
                         Foreground=""#AFC8EA"" VerticalAlignment=""Center""/>
              <Border Margin=""10,0,0,0"" CornerRadius=""4"" Background=""#0D1B33"" BorderBrush=""#24406E""
                      BorderThickness=""1"" Padding=""7,1"" VerticalAlignment=""Center"">
                <TextBlock Text=""v2.1.1"" FontSize=""9.5"" Foreground=""#C2D2EC""/>
              </Border>
            </StackPanel>
          </DockPanel>
        </Border>

        <!-- ==================== 主体 ==================== -->
        <Grid Grid.Row=""1"" Margin=""28,2,28,16"">
          <Grid.RowDefinitions>
            <RowDefinition Height=""Auto""/>
            <RowDefinition Height=""Auto""/>
            <RowDefinition Height=""*""/>
            <RowDefinition Height=""Auto""/>
            <RowDefinition Height=""Auto""/>
          </Grid.RowDefinitions>

          <!-- 头部 -->
          <Grid Grid.Row=""0"">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width=""*""/>
              <ColumnDefinition Width=""Auto""/>
            </Grid.ColumnDefinitions>
            <StackPanel>
              <TextBlock Text=""S Y S T E M   P O W E R   C O N T R O L"" FontSize=""9.5"" FontWeight=""SemiBold""
                         Foreground=""#3FBBE0"" Opacity=""0.85""/>
              <TextBlock Margin=""0,5,0,0"" Text=""智能关机控制台"" FontSize=""24"" FontWeight=""Bold"" Foreground=""#F2F7FF""/>
            </StackPanel>
            <StackPanel Grid.Column=""1"" Orientation=""Horizontal"" VerticalAlignment=""Center"">
              <Ellipse x:Name=""LedDot"" Width=""10"" Height=""10"" Fill=""#3BE08A"">
                <Ellipse.Effect>
                  <DropShadowEffect Color=""#3BE08A"" BlurRadius=""10"" ShadowDepth=""0"" Opacity=""0.9""/>
                </Ellipse.Effect>
              </Ellipse>
              <TextBlock x:Name=""LedText"" Margin=""9,0,0,0"" Text=""系统待机 · STANDBY""
                         FontSize=""12.5"" Foreground=""#CBD9F2"" VerticalAlignment=""Center""/>
            </StackPanel>
          </Grid>

          <!-- 分段选项卡 -->
          <Border Grid.Row=""1"" Margin=""0,16,0,0"" Background=""#0A1424"" BorderBrush=""#1C3050""
                  BorderThickness=""1"" CornerRadius=""10"" Padding=""4"" Width=""400"" HorizontalAlignment=""Left"">
            <Grid>
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width=""*""/>
                <ColumnDefinition Width=""*""/>
              </Grid.ColumnDefinitions>
              <RadioButton x:Name=""TabCnt"" Style=""{StaticResource TabBtn}"" IsChecked=""True"">
                <StackPanel Orientation=""Horizontal"">
                  <TextBlock Text=""&#xE916;"" FontFamily=""Segoe MDL2 Assets"" FontSize=""13""
                             VerticalAlignment=""Center"" Margin=""0,0,8,0""/>
                  <TextBlock Text=""倒计时关机"" VerticalAlignment=""Center""/>
                </StackPanel>
              </RadioButton>
              <RadioButton Grid.Column=""1"" x:Name=""TabSched"" Style=""{StaticResource TabBtn}"">
                <StackPanel Orientation=""Horizontal"">
                  <TextBlock Text=""&#xE823;"" FontFamily=""Segoe MDL2 Assets"" FontSize=""13""
                             VerticalAlignment=""Center"" Margin=""0,0,8,0""/>
                  <TextBlock Text=""定时关机"" VerticalAlignment=""Center""/>
                </StackPanel>
              </RadioButton>
            </Grid>
          </Border>

          <!-- 内容区 -->
          <Grid Grid.Row=""2"" Margin=""0,18,0,0"">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width=""250""/>
              <ColumnDefinition Width=""*""/>
            </Grid.ColumnDefinitions>

            <!-- 环形进度 -->
            <Grid Width=""220"" Height=""220"" VerticalAlignment=""Top"" HorizontalAlignment=""Center"">
              <Ellipse Stroke=""#13233E"" StrokeThickness=""10""/>
              <Canvas x:Name=""TicksCanvas"" Width=""220"" Height=""220""/>
              <Path x:Name=""ArcProgress"" StrokeThickness=""10"" StrokeStartLineCap=""Round""
                    StrokeEndLineCap=""Round"" Visibility=""Collapsed"">
                <Path.Stroke>
                  <LinearGradientBrush StartPoint=""0,0"" EndPoint=""1,1"">
                    <GradientStop Color=""#2BE4FF"" Offset=""0""/>
                    <GradientStop Color=""#3E8BFF"" Offset=""1""/>
                  </LinearGradientBrush>
                </Path.Stroke>
                <Path.Effect>
                  <DropShadowEffect Color=""#2BE4FF"" BlurRadius=""16"" ShadowDepth=""0"" Opacity=""0.65""/>
                </Path.Effect>
              </Path>
              <StackPanel VerticalAlignment=""Center"" HorizontalAlignment=""Center"">
                <TextBlock x:Name=""RingTime"" Text=""--:--:--"" FontFamily=""Cascadia Mono, Consolas""
                           FontSize=""30"" FontWeight=""Bold"" Foreground=""#F2F7FF"" HorizontalAlignment=""Center"">
                  <TextBlock.Effect>
                    <DropShadowEffect Color=""#2BE4FF"" BlurRadius=""18"" ShadowDepth=""0"" Opacity=""0.35""/>
                  </TextBlock.Effect>
                </TextBlock>
                <TextBlock Margin=""0,3,0,0"" Text=""R E M A I N I N G"" FontSize=""9""
                           Foreground=""#98ADD3"" HorizontalAlignment=""Center""/>
                <Border Margin=""0,8,0,0"" CornerRadius=""4"" Background=""#0D1C33"" BorderBrush=""#22456F""
                        BorderThickness=""1"" Padding=""8,2"" HorizontalAlignment=""Center"">
                  <TextBlock x:Name=""RingMode"" Text=""待机 STANDBY"" FontSize=""9"" Foreground=""#74D9FF""/>
                </Border>
              </StackPanel>
            </Grid>

            <!-- 右侧控制区 -->
            <Grid Grid.Column=""1"" Margin=""26,0,0,0"">
              <Grid.RowDefinitions>
                <RowDefinition Height=""Auto""/>
                <RowDefinition Height=""Auto""/>
              </Grid.RowDefinitions>

              <Grid Grid.Row=""0"">
                <!-- 倒计时面板 -->
                <StackPanel x:Name=""PanelCnt"">
                  <TextBlock Style=""{StaticResource Lbl}"" Text=""预 设 间 隔 · QUICK PRESETS""/>
                  <WrapPanel x:Name=""ChipPanelCnt"">
                    <Button Style=""{StaticResource ChipBtn}"" Content=""15 分钟"" Tag=""0,15,0""/>
                    <Button Style=""{StaticResource ChipBtn}"" Content=""30 分钟"" Tag=""0,30,0""/>
                    <Button Style=""{StaticResource ChipBtn}"" Content=""1 小时"" Tag=""1,0,0""/>
                    <Button Style=""{StaticResource ChipBtn}"" Content=""2 小时"" Tag=""2,0,0""/>
                    <Button Style=""{StaticResource ChipBtn}"" Content=""4 小时"" Tag=""4,0,0""/>
                  </WrapPanel>
                  <TextBlock Style=""{StaticResource Lbl}"" Margin=""0,10,0,8"" Text=""自 定 时 长 · 点击 ▲▼ 或直接输入""/>
                  <StackPanel x:Name=""CntSteps"" Orientation=""Horizontal""/>
                  <TextBlock x:Name=""ErrCnt"" Margin=""0,8,0,0"" MinHeight=""16"" FontSize=""11""
                             Foreground=""#FF7A90"" TextWrapping=""Wrap"" Text="""" LineHeight=""15""/>
                  <Button x:Name=""BtnCntStart"" Style=""{StaticResource PrimaryBtn}"" Margin=""0,4,0,0"">
                    <StackPanel Orientation=""Horizontal"">
                      <TextBlock Text=""&#xE7E8;"" FontFamily=""Segoe MDL2 Assets"" FontSize=""15""
                                 VerticalAlignment=""Center"" Margin=""0,0,9,0""/>
                      <TextBlock Text=""启动倒计时关机"" VerticalAlignment=""Center""/>
                    </StackPanel>
                  </Button>
                  <Button x:Name=""BtnCntCancel"" Style=""{StaticResource GhostBtn}"" Margin=""0,10,0,0""
                          IsEnabled=""False"" Content=""取消关机计划（shutdown /a）""/>
                </StackPanel>

                <!-- 定时面板 -->
                <StackPanel x:Name=""PanelSched"" Visibility=""Collapsed"">
                  <TextBlock Style=""{StaticResource Lbl}"" Text=""目 标 时 刻 · TARGET TIME (24H)""/>
                  <StackPanel Orientation=""Horizontal"">
                    <StackPanel x:Name=""SchedSteps"" Orientation=""Horizontal"" VerticalAlignment=""Center""/>
                    <StackPanel Margin=""14,0,0,0"" VerticalAlignment=""Center"">
                      <TextBlock Text=""点击 ▲▼ 或直接输入（小时 00-23，分钟 00-59）"" FontSize=""11"" Foreground=""#C2D2EC""/>
                      <TextBlock Margin=""0,3,0,0"" Text=""早于当前时刻则视为次日执行"" FontSize=""10.5"" Foreground=""#98ADD3""/>
                    </StackPanel>
                  </StackPanel>
                  <TextBlock Style=""{StaticResource Lbl}"" Margin=""0,14,0,8"" Text=""常 用 时 刻 · SHORTCUTS""/>
                  <WrapPanel x:Name=""ChipPanelSched"">
                    <Button Style=""{StaticResource ChipBtn}"" Content=""21:00"" Tag=""21:00""/>
                    <Button Style=""{StaticResource ChipBtn}"" Content=""22:00"" Tag=""22:00""/>
                    <Button Style=""{StaticResource ChipBtn}"" Content=""23:30"" Tag=""23:30""/>
                    <Button Style=""{StaticResource ChipBtn}"" Content=""00:00"" Tag=""00:00""/>
                    <Button Style=""{StaticResource ChipBtn}"" Content=""01:00"" Tag=""01:00""/>
                  </WrapPanel>
                  <TextBlock x:Name=""SchedInfo"" Margin=""0,4,0,0"" MinHeight=""16"" FontSize=""11""
                             Foreground=""#74D9FF"" Text="""" LineHeight=""15""/>
                  <TextBlock x:Name=""ErrSched"" Margin=""0,0,0,0"" MinHeight=""16"" FontSize=""11""
                             Foreground=""#FF7A90"" TextWrapping=""Wrap"" Text="""" LineHeight=""15""/>
                  <Button x:Name=""BtnSchedStart"" Style=""{StaticResource PrimaryBtn}"" Margin=""0,4,0,0"">
                    <StackPanel Orientation=""Horizontal"">
                      <TextBlock Text=""&#xE7E8;"" FontFamily=""Segoe MDL2 Assets"" FontSize=""15""
                                 VerticalAlignment=""Center"" Margin=""0,0,9,0""/>
                      <TextBlock Text=""启动定时关机"" VerticalAlignment=""Center""/>
                    </StackPanel>
                  </Button>
                  <Button x:Name=""BtnSchedCancel"" Style=""{StaticResource GhostBtn}"" Margin=""0,10,0,0""
                          IsEnabled=""False"" Content=""取消关机计划（shutdown /a）""/>
                </StackPanel>
              </Grid>
            </Grid>

            <!-- HUD 角标 -->
            <Border Width=""16"" Height=""16"" HorizontalAlignment=""Left"" VerticalAlignment=""Top""
                    BorderBrush=""#2BE4FF"" BorderThickness=""1.5,1.5,0,0"" Opacity=""0.5"" IsHitTestVisible=""False""/>
            <Border Width=""16"" Height=""16"" HorizontalAlignment=""Right"" VerticalAlignment=""Top""
                    BorderBrush=""#2BE4FF"" BorderThickness=""0,1.5,1.5,0"" Opacity=""0.5"" IsHitTestVisible=""False""/>
            <Border Width=""16"" Height=""16"" HorizontalAlignment=""Left"" VerticalAlignment=""Bottom""
                    BorderBrush=""#2BE4FF"" BorderThickness=""1.5,0,0,1.5"" Opacity=""0.5"" IsHitTestVisible=""False""/>
            <Border Width=""16"" Height=""16"" HorizontalAlignment=""Right"" VerticalAlignment=""Bottom""
                    BorderBrush=""#2BE4FF"" BorderThickness=""0,0,1.5,1.5"" Opacity=""0.5"" IsHitTestVisible=""False""/>
          </Grid>

          <!-- 提示行 -->
          <TextBlock x:Name=""PlanLine"" Grid.Row=""3"" Margin=""0,14,0,0"" FontSize=""10.5"" Foreground=""#98ADD3""
                     TextTrimming=""CharacterEllipsis""
                     Text=""提示：关机计划由 Windows shutdown 指令在系统层执行，退出本程序不会取消已提交的计划。""/>

          <!-- 状态栏 -->
          <StackPanel Grid.Row=""4"" Margin=""0,12,0,0"">
            <Rectangle Height=""1"" Fill=""#14243F""/>
            <Grid Margin=""0,10,0,0"">
              <TextBlock Text=""SHUTDOWN EXECUTOR · /S /T SECONDS · CANCEL VIA /A""
                         FontSize=""9.5"" Foreground=""#7A90B8"" VerticalAlignment=""Center""/>
              <StackPanel Orientation=""Horizontal"" HorizontalAlignment=""Right"">
                <TextBlock x:Name=""ClockText"" Text="""" FontFamily=""Cascadia Mono, Consolas""
                           FontSize=""12.5"" Foreground=""#B0D2F8"" VerticalAlignment=""Center""/>
                <Button x:Name=""BtnAbort"" Style=""{StaticResource AbortBtn}"" Margin=""14,0,0,0""
                        IsEnabled=""False"" Content=""取消系统关机""/>
              </StackPanel>
            </Grid>
          </StackPanel>

        </Grid>
      </Grid>
    </Border>
  </Grid>
</Window>";
    }

    // ======================= 步进数字输入 =======================
    // 离散式输入：可直接键入（超范围即时钳制），也可点击 ▲▼ / 滚轮 / 上下键调节
    internal sealed class TimeStepper : Border
    {
        private readonly TextBox _box = new TextBox();
        private readonly RepeatButton _up = new RepeatButton();
        private readonly RepeatButton _down = new RepeatButton();

        public int Min { get; private set; }
        public int Max { get; private set; }
        public int Step { get; set; }

        public event Action Changed;

        public int Value
        {
            get { int v; return int.TryParse(_box.Text, out v) ? v : 0; }
        }

        public double BoxFontSize
        {
            set { _box.FontSize = value; }
        }

        public TimeStepper(Style boxStyle, int width, int height, int max, int step = 1)
        {
            Min = 0; Max = max; Step = step;
            Width = width; Height = height;

            CornerRadius = new CornerRadius(8);
            Background = Brush("#0A1322");
            BorderBrush = Brush("#24406E");
            BorderThickness = new Thickness(1);
            SnapsToDevicePixels = true;
            MouseEnter += (s, e) => BorderBrush = Brush("#2BE4FF");
            MouseLeave += (s, e) => BorderBrush = Brush("#24406E");

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(15) });

            _box.Style = boxStyle;
            _box.Margin = new Thickness(0, 0, 1, 0);
            _box.MaxLength = Max.ToString().Length;
            _box.TextChanged += OnTextChanged;
            _box.LostFocus += (s, e) => _box.Text = Value.ToString();
            _box.GotKeyboardFocus += (s, e) => _box.SelectAll();
            _box.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Up) { Bump(1); e.Handled = true; }
                else if (e.Key == Key.Down) { Bump(-1); e.Handled = true; }
            };
            Grid.SetColumn(_box, 0);
            grid.Children.Add(_box);

            var arrows = new Grid
            {
                Width = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 3, 0)
            };
            arrows.RowDefinitions.Add(new RowDefinition());
            arrows.RowDefinitions.Add(new RowDefinition());
            ConfigureArrow(_up, "\uE70E");
            ConfigureArrow(_down, "\uE70D");
            _up.Delay = 350; _up.Interval = 80;
            _down.Delay = 350; _down.Interval = 80;
            _up.Click += (s, e) => Bump(1);
            _down.Click += (s, e) => Bump(-1);
            Grid.SetRow(_up, 0);
            Grid.SetRow(_down, 1);
            arrows.Children.Add(_up);
            arrows.Children.Add(_down);
            Grid.SetColumn(arrows, 1);
            grid.Children.Add(arrows);

            Child = grid;
            MouseWheel += (s, e) => { Bump(e.Delta > 0 ? 1 : -1); e.Handled = true; };
        }

        public void SetValue(int v)
        {
            _box.Text = Clamp(v).ToString();
        }

        public void SimulateType(string text)
        {
            _box.Text = text;
        }

        public void Bump(int direction)
        {
            int v = Value + direction * Step;
            if (v > Max) v = Min;
            if (v < Min) v = Max;
            SetValue(v);
            _box.CaretIndex = _box.Text.Length;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            string t = _box.Text ?? "";
            string clean = new string(t.Where(char.IsDigit).ToArray());
            if (clean != t)
            {
                _box.Text = clean;   // 触发一次重入，走钳制分支
                return;
            }
            int v;
            if (clean.Length > 0 && int.TryParse(clean, out v) && (v > Max || v < Min))
            {
                _box.Text = Clamp(v).ToString();
                _box.CaretIndex = _box.Text.Length;
                return;
            }
            var handler = Changed;
            if (handler != null) handler();
        }

        private int Clamp(int v) { return v < Min ? Min : (v > Max ? Max : v); }

        private static void ConfigureArrow(RepeatButton btn, string glyph)
        {
            var tpl = new ControlTemplate(typeof(RepeatButton));
            var bd = new FrameworkElementFactory(typeof(Border), "bd");
            bd.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            bd.AppendChild(cp);
            tpl.VisualTree = bd;
            var trig = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            trig.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#1A2A47"), "bd"));
            trig.Setters.Add(new Setter(Control.ForegroundProperty, Brush("#2BE4FF")));
            tpl.Triggers.Add(trig);
            btn.Template = tpl;
            btn.Foreground = Brush("#B0C2E4");
            btn.Cursor = Cursors.Hand;
            btn.Content = new TextBlock
            {
                Text = glyph,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 8
            };
        }

        internal static SolidColorBrush Brush(string hex)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }
    }

    // ============================ 主逻辑 ============================
    internal sealed class Controller
    {
        private readonly Window _win;
        private bool _selfTest;

        private TimeStepper _cntH, _cntM, _cntS, _schH, _schM;

        private RadioButton _tabCnt, _tabSched;
        private StackPanel _panelCnt, _panelSched;
        private Button _btnCntStart, _btnSchedStart, _btnCntCancel, _btnSchedCancel, _btnAbort, _btnMin, _btnClose;
        private TextBlock _ringTime, _ringMode, _ledText, _planLine, _clockText, _schedInfo, _errCnt, _errSched;
        private Ellipse _ledDot;
        private System.Windows.Shapes.Path _arc;
        private Canvas _ticks;

        private readonly DispatcherTimer _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

        private bool _armed;
        private bool _modeScheduled;
        private bool _blink;
        private TimeSpan _total;
        private DateTime _target;

        public Controller(Window win)
        {
            _win = win;
            _selfTest = false;

            _tabCnt = Find<RadioButton>("TabCnt"); _tabSched = Find<RadioButton>("TabSched");
            _panelCnt = Find<StackPanel>("PanelCnt"); _panelSched = Find<StackPanel>("PanelSched");
            _btnCntStart = Find<Button>("BtnCntStart"); _btnSchedStart = Find<Button>("BtnSchedStart");
            _btnCntCancel = Find<Button>("BtnCntCancel"); _btnSchedCancel = Find<Button>("BtnSchedCancel");
            _btnAbort = Find<Button>("BtnAbort"); _btnMin = Find<Button>("BtnMin"); _btnClose = Find<Button>("BtnClose");
            _ringTime = Find<TextBlock>("RingTime"); _ringMode = Find<TextBlock>("RingMode");
            _ledText = Find<TextBlock>("LedText"); _planLine = Find<TextBlock>("PlanLine");
            _clockText = Find<TextBlock>("ClockText"); _schedInfo = Find<TextBlock>("SchedInfo");
            _errCnt = Find<TextBlock>("ErrCnt"); _errSched = Find<TextBlock>("ErrSched");
            _ledDot = Find<Ellipse>("LedDot"); _arc = Find<System.Windows.Shapes.Path>("ArcProgress");
            _ticks = Find<Canvas>("TicksCanvas");
            var titleBar = Find<Border>("TitleBar");
            var chipCnt = Find<WrapPanel>("ChipPanelCnt");
            var chipSched = Find<WrapPanel>("ChipPanelSched");
            var cntSteps = Find<StackPanel>("CntSteps");
            var schedSteps = Find<StackPanel>("SchedSteps");

            Style boxStyle = (Style)_win.FindResource("StepBox");

            // ---- 倒计时：时(0-999) / 分(0-59) / 秒(0-59) ----
            _cntH = new TimeStepper(boxStyle, 64, 38, 999);
            _cntM = new TimeStepper(boxStyle, 64, 38, 59);
            _cntS = new TimeStepper(boxStyle, 64, 38, 59);
            _cntH.SetValue(0); _cntM.SetValue(30); _cntS.SetValue(0);
            AddCountdownRow(cntSteps, new[] { _cntH, _cntM, _cntS }, new[] { "时", "分", "秒" });

            // ---- 定时：时(0-23) / 分(0-59)，输入即时钳制 ----
            _schH = new TimeStepper(boxStyle, 76, 46, 23);
            _schM = new TimeStepper(boxStyle, 76, 46, 59);
            _schH.BoxFontSize = 24; _schM.BoxFontSize = 24;
            var t0 = DateTime.Now.AddHours(1);
            _schH.SetValue(t0.Hour); _schM.SetValue(0);
            var colon = new TextBlock
            {
                Text = ":",
                FontFamily = new FontFamily("Cascadia Mono, Consolas"),
                FontSize = 24,
                Foreground = TimeStepper.Brush("#98ADD3"),
                Margin = new Thickness(5, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            schedSteps.Children.Add(_schH);
            schedSteps.Children.Add(colon);
            schedSteps.Children.Add(_schM);
            _schH.Changed += UpdateSchedPreview;
            _schM.Changed += UpdateSchedPreview;

            // ---- 刻度环 ----
            for (int i = 0; i < 60; i++)
            {
                double ang = (i * 6 - 90) * Math.PI / 180;
                bool major = i % 5 == 0;
                double r1 = major ? 88 : 92, r2 = 96;
                var line = new Line
                {
                    X1 = 110 + r1 * Math.Cos(ang), Y1 = 110 + r1 * Math.Sin(ang),
                    X2 = 110 + r2 * Math.Cos(ang), Y2 = 110 + r2 * Math.Sin(ang),
                    Stroke = major ? TimeStepper.Brush("#355182") : TimeStepper.Brush("#253A60"),
                    StrokeThickness = major ? 1.4 : 1
                };
                _ticks.Children.Add(line);
            }

            // ---- 事件 ----
            titleBar.MouseLeftButtonDown += (s, e) => { try { _win.DragMove(); } catch { } };
            _btnClose.Click += (s, e) => _win.Close();
            _btnMin.Click += (s, e) => _win.WindowState = WindowState.Minimized;

            _tabCnt.Checked += (s, e) => { _panelCnt.Visibility = Visibility.Visible; _panelSched.Visibility = Visibility.Collapsed; };
            _tabSched.Checked += (s, e) => { _panelSched.Visibility = Visibility.Visible; _panelCnt.Visibility = Visibility.Collapsed; UpdateSchedPreview(); };

            foreach (var o in chipCnt.Children)
            {
                var b = o as Button;
                if (b == null) continue;
                b.Click += (s, e) =>
                {
                    var parts = (b.Tag as string ?? "").Split(',');
                    int h, m, sec;
                    int.TryParse(parts.Length > 0 ? parts[0] : "0", out h);
                    int.TryParse(parts.Length > 1 ? parts[1] : "0", out m);
                    int.TryParse(parts.Length > 2 ? parts[2] : "0", out sec);
                    _cntH.SetValue(h); _cntM.SetValue(m); _cntS.SetValue(sec);
                    ClearErrs();
                };
            }
            foreach (var o in chipSched.Children)
            {
                var b = o as Button;
                if (b == null) continue;
                b.Click += (s, e) =>
                {
                    var parts = (b.Tag as string ?? "").Split(':');
                    int h, m;
                    int.TryParse(parts.Length > 0 ? parts[0] : "0", out h);
                    int.TryParse(parts.Length > 1 ? parts[1] : "0", out m);
                    _schH.SetValue(h); _schM.SetValue(m);
                    UpdateSchedPreview(); ClearErrs();
                };
            }

            _btnCntStart.Click += (s, e) => StartPlan(false);
            _btnSchedStart.Click += (s, e) => StartPlan(true);
            _btnCntCancel.Click += (s, e) => StopPlan("manual");
            _btnSchedCancel.Click += (s, e) => StopPlan("manual");
            _btnAbort.Click += (s, e) => StopPlan("manual");

            _win.Closing += (s, e) =>
            {
                if (!_armed) return;
                string msg = string.Format(
                    "关机计划仍在进行：{0}\n\n「是」 — 取消关机计划并退出\n「否」 — 保留关机计划并退出\n「取消」 — 返回程序",
                    _target.ToString("yyyy-MM-dd HH:mm:ss"));
                var r = MessageBox.Show(_win, msg, "退出确认", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (r == MessageBoxResult.Yes) StopPlan("silent");
                else if (r == MessageBoxResult.Cancel) e.Cancel = true;
            };

            _timer.Tick += (s, e) => UpdateTick();
            _timer.Start();

            ResetUi();
            UpdateSchedPreview();
            UpdateTick();
        }

        private T Find<T>(string name) where T : class
        {
            var o = _win.FindName(name);
            if (o == null) throw new InvalidOperationException("未找到控件: " + name);
            return (T)o;
        }

        private void AddCountdownRow(StackPanel panel, TimeStepper[] steppers, string[] labels)
        {
            // 版式：（数值）时（数值）分（数值）秒 —— 单位跟在输入框后面
            for (int i = 0; i < steppers.Length; i++)
            {
                panel.Children.Add(steppers[i]);
                panel.Children.Add(new TextBlock
                {
                    Text = labels[i],
                    FontSize = 12,
                    Foreground = TimeStepper.Brush("#C2D2EC"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 0, 0)
                });
            }
        }

        private static string Fmt(TimeSpan t)
        {
            long h = (long)t.TotalHours;
            if (h > 999) h = 999;
            return string.Format("{0:00}:{1:00}:{2:00}", h, t.Minutes, t.Seconds);
        }

        private void SetArc(double fraction)
        {
            if (fraction >= 0.9995)
            {
                _arc.Visibility = Visibility.Visible;
                _arc.Data = new EllipseGeometry(new Point(110, 110), 105, 105);
                return;
            }
            if (fraction <= 0.002) { _arc.Visibility = Visibility.Collapsed; return; }
            _arc.Visibility = Visibility.Visible;
            double a = 2 * Math.PI * fraction;
            var fig = new PathFigure { StartPoint = new Point(110, 5), IsClosed = false };
            var seg = new ArcSegment
            {
                Point = new Point(110 + 105 * Math.Sin(a), 110 - 105 * Math.Cos(a)),
                Size = new Size(105, 105),
                IsLargeArc = fraction > 0.5,
                SweepDirection = SweepDirection.Clockwise
            };
            fig.Segments.Add(seg);
            _arc.Data = new PathGeometry(new[] { fig });
        }

        private void ShowErr(bool sched, string msg)
        {
            if (sched) _errSched.Text = msg; else _errCnt.Text = msg;
        }

        private void ClearErrs()
        {
            _errCnt.Text = ""; _errSched.Text = "";
        }

        private void InvokeShutdown(int seconds, bool scheduled)
        {
            if (_selfTest) return;
            string comment = scheduled ? "系统已在计划时刻到达，即将自动关机" : "倒计时结束，系统即将自动关机";
            Process.Start(new ProcessStartInfo("shutdown.exe", "/s /t " + seconds + " /c " + comment)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }

        private void AbortShutdown()
        {
            if (_selfTest) return;
            try
            {
                Process.Start(new ProcessStartInfo("shutdown.exe", "/a")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            catch { }
        }

        private void ResetUi()
        {
            _armed = false;
            _total = TimeSpan.Zero;
            _btnCntStart.IsEnabled = true;
            _btnSchedStart.IsEnabled = true;
            _btnCntCancel.IsEnabled = false;
            _btnSchedCancel.IsEnabled = false;
            _btnAbort.IsEnabled = false;
            _ledDot.Fill = TimeStepper.Brush("#3BE08A");
            ((DropShadowEffect)_ledDot.Effect).Color = Color.FromRgb(0x3B, 0xE0, 0x8A);
            _ledDot.Opacity = 1;
            _ledText.Text = "系统待机 · STANDBY";
            _ledText.Foreground = TimeStepper.Brush("#CBD9F2");
            _ringTime.Text = "--:--:--";
            _ringMode.Text = "待机 STANDBY";
            SetArc(0);
            _planLine.Text = "提示：关机计划由 Windows shutdown 指令在系统层执行，退出本程序不会取消已提交的计划。";
        }

        public void StartPlan(bool scheduled)
        {
            ClearErrs();
            DateTime now = DateTime.Now;
            if (!scheduled)
            {
                _total = new TimeSpan(_cntH.Value, _cntM.Value, _cntS.Value);
                if (_total.TotalSeconds < 60)
                {
                    ShowErr(false, "时长无效：请至少设置 60 秒（可点击 ▲▼ 或直接输入任意时长）");
                    return;
                }
                _target = now.AddSeconds(_total.TotalSeconds);
            }
            else
            {
                _target = now.Date.AddHours(_schH.Value).AddMinutes(_schM.Value);
                if (_target <= now) _target = _target.AddDays(1);
                _total = _target - now;
                if (_total.TotalSeconds < 60) { ShowErr(true, "距离目标时刻不足 60 秒，请调整时间"); return; }
            }

            try { InvokeShutdown((int)Math.Ceiling(_total.TotalSeconds), scheduled); }
            catch (Exception ex) { ShowErr(scheduled, "无法提交系统关机指令：" + ex.Message); return; }

            _armed = true;
            _modeScheduled = scheduled;
            _blink = false;
            _btnCntStart.IsEnabled = false;
            _btnSchedStart.IsEnabled = false;
            _btnCntCancel.IsEnabled = true;
            _btnSchedCancel.IsEnabled = true;
            _btnAbort.IsEnabled = true;
            _ledDot.Fill = TimeStepper.Brush("#2BE4FF");
            ((DropShadowEffect)_ledDot.Effect).Color = Color.FromRgb(0x2B, 0xE4, 0xFF);
            _ledText.Foreground = TimeStepper.Brush("#CFE6FF");
            _ringMode.Text = scheduled ? "定时 SCHEDULED" : "倒计时 COUNTDOWN";
            UpdateTick();
        }

        public void StopPlan(string reason)
        {
            AbortShutdown();
            ResetUi();
            if (reason == "manual")
            {
                _ledDot.Fill = TimeStepper.Brush("#FFB454");
                ((DropShadowEffect)_ledDot.Effect).Color = Color.FromRgb(0xFF, 0xB4, 0x54);
                _ledText.Text = "已取消关机计划 · CANCELLED";
                _ledText.Foreground = TimeStepper.Brush("#FFB454");
                _planLine.Text = "已执行 shutdown /a，系统关机计划已撤销。";
            }
        }

        public void UpdateSchedPreview()
        {
            if (_armed && _modeScheduled) return;
            DateTime now = DateTime.Now;
            DateTime t = now.Date.AddHours(_schH.Value).AddMinutes(_schM.Value);
            if (t <= now) t = t.AddDays(1);
            _schedInfo.Text = string.Format("目标 {0} · 约 {1} 后关机",
                t.ToString("yyyy-MM-dd HH:mm"), Fmt(t - now));
        }

        public void UpdateTick()
        {
            DateTime now = DateTime.Now;
            _clockText.Text = now.ToString("yyyy-MM-dd  HH:mm:ss");
            if (!_armed) return;

            _blink = !_blink;
            _ledDot.Opacity = _blink ? 1 : 0.35;
            TimeSpan rem = _target - now;
            if (rem.TotalSeconds <= 0)
            {
                SetArc(0);
                _ringTime.Text = "00:00:00";
                _ledText.Text = "关机指令执行中 · SHUTTING DOWN";
                _ledText.Foreground = TimeStepper.Brush("#FF9AAE");
                _planLine.Text = "目标时刻 " + _target.ToString("yyyy-MM-dd HH:mm:ss") + " 已到达，系统正在执行关机。";
            }
            else
            {
                SetArc(rem.TotalSeconds / _total.TotalSeconds);
                _ringTime.Text = Fmt(rem);
                _ledText.Text = string.Format("已计划 {0} 关机 · 剩余 {1}",
                    _target.ToString("MM-dd HH:mm"), Fmt(rem));
                _planLine.Text = "系统关机计划已提交 · 目标 " + _target.ToString("yyyy-MM-dd HH:mm:ss") +
                                 " · 点击「取消关机计划」可随时撤销";
                if (_modeScheduled)
                    _schedInfo.Text = string.Format("目标 {0} · 剩余 {1}",
                        _target.ToString("yyyy-MM-dd HH:mm"), Fmt(rem));
            }
        }

        // ======================= 自测与截图 =======================
        public void RunSelfTest(bool withShots)
        {
            _selfTest = true;
            var sb = new StringBuilder();
            int fails = 0;
            Action<string, bool> check = (name, ok) =>
            {
                sb.AppendLine((ok ? "[PASS] " : "[FAIL] ") + name);
                if (!ok) fails++;
            };
            try
            {
                _schH.SimulateType("36");
                check("定时-小时输入 36 被即时钳制为 23（实际 " + _schH.Value + "）", _schH.Value == 23);
                _schM.SimulateType("75");
                check("定时-分钟输入 75 被即时钳制为 59（实际 " + _schM.Value + "）", _schM.Value == 59);

                _schH.SetValue(23); _schH.Bump(1);
                check("定时-小时 23 递增后回绕到 0（实际 " + _schH.Value + "）", _schH.Value == 0);

                _cntH.SetValue(1); _cntM.SetValue(23); _cntS.SetValue(45);
                StartPlan(false);
                check("倒计时 1h23m45s = 5025 秒（实际 " + _total.TotalSeconds + "）",
                      Math.Abs(_total.TotalSeconds - 5025) < 1);
                check("格式化输出 01:23:45", Fmt(_total) == "01:23:45");
                check("目标时刻 = 当前 + 约 5025 秒",
                      Math.Abs((_target - DateTime.Now).TotalSeconds - 5024) < 3);

                StopPlan("silent");
                _tabSched.IsChecked = true;
                _schH.SetValue(23); _schM.SetValue(30);
                StartPlan(true); UpdateTick();
                check("定时目标为 23:30", _target.Hour == 23 && _target.Minute == 30);
                check("定时目标在未来", _target > DateTime.Now);
                check("定时总时长 ≤ 86400 秒", _total.TotalSeconds <= 86401);
                check("剩余时间显示已更新（" + _ringTime.Text + "）", _ringTime.Text.Length >= 8);

                SetArc(0.25); check("圆弧 25% 渲染", _arc.Data != null);
                SetArc(1.0); check("圆弧满环渲染", _arc.Data != null);
                SetArc(0); check("圆弧归零收起", _arc.Visibility == Visibility.Collapsed);
            }
            catch (Exception ex)
            {
                fails++;
                sb.AppendLine("[FAIL] exception: " + ex);
            }

            if (withShots)
            {
                try { RenderShots(); sb.AppendLine("[PASS] SHOTS RENDERED"); }
                catch (Exception ex) { fails++; sb.AppendLine("[FAIL] shots: " + ex); }
            }

            sb.AppendLine(fails == 0 ? "RESULT: ALL PASS" : "RESULT: " + fails + " FAILED");
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "selftest_result.txt");
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
            try { if (_win.IsLoaded) { StopPlan("silent"); _win.Close(); } } catch { }
            Environment.Exit(fails == 0 ? 0 : 2);
        }

        private void RenderShots()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            _win.WindowStartupLocation = WindowStartupLocation.Manual;
            _win.Left = -32000; _win.Top = 0;
            _win.Show();
            _win.Measure(new Size(880, 600));
            _win.Arrange(new Rect(0, 0, 880, 600));
            _win.UpdateLayout();
            Pump();
            _tabCnt.IsChecked = true;   // 自测阶段可能停留在定时页，重置回倒计时
            StopPlan("silent");         // 清除自测残留的“已计划”状态
            _cntH.SetValue(0); _cntM.SetValue(30); _cntS.SetValue(0);
            _win.UpdateLayout();
            Pump();

            Snap(System.IO.Path.Combine(dir, "_shot1_idle_countdown.png"));

            _cntH.SetValue(1); _cntM.SetValue(23); _cntS.SetValue(45);
            StartPlan(false); UpdateTick();
            Snap(System.IO.Path.Combine(dir, "_shot2_armed_countdown.png"));

            StopPlan("silent");
            _tabSched.IsChecked = true; UpdateSchedPreview();
            Snap(System.IO.Path.Combine(dir, "_shot3_idle_sched.png"));

            _schH.SetValue(23); _schM.SetValue(30);
            StartPlan(true); UpdateTick();
            Snap(System.IO.Path.Combine(dir, "_shot4_armed_sched.png"));

            StopPlan("silent");
        }

        private static void Pump()
        {
            var d = Dispatcher.CurrentDispatcher;
            d.Invoke(new Action(() => { }), DispatcherPriority.Loaded);
            d.Invoke(new Action(() => { }), DispatcherPriority.Background);
        }

        private void Snap(string path)
        {
            _win.UpdateLayout();
            Pump();
            var rtb = new RenderTargetBitmap(880, 600, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(_win.Content as Visual);
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(rtb));
            using (var fs = File.Create(path)) enc.Save(fs);
        }
    }
}
