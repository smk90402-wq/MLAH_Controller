using DevExpress.Xpf.Map;
using System;
using System.ComponentModel; // ✅ 추가
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MLAH_Controller
{
    public class LocalTileSource : ImageTileSource
    {

        private static readonly WriteableBitmap EmptyTile = new WriteableBitmap(256, 256, 96, 96, PixelFormats.Pbgra32, null);

        // ✅ 생성자를 추가하여 디자인 모드일 때 미리 확인 (더 안정적인 방법)
        public LocalTileSource()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return; // 디자인 모드에서는 아무것도 하지 않음
            }
        }

        public override ImageSource GetImageSource(long x, long y, int level, Size tileSize)
        {
            // ✅ 메서드 시작 부분에도 디자인 모드 확인 로직을 추가
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return null; // 디자이너에서는 타일을 로드하지 않음
            }

            //REM-- - 폴더 복사(Robocopy)-- -
            //REM Tiles 폴더를 출력 폴더로 복사합니다. (기존 명령어와 동일)
            //robocopy "$(ProjectDir)Resources\Tiles_0623_14" "$(TargetDir)Tiles_0623_14" / E / R:0 / W:0 / XO


            var path = PathManager.GetOrSetLibraryPath();

            //string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            //string tileDir = Path.Combine(baseDir, "Tiles_0623_14", level.ToString());
            string tileDir = Path.Combine(path, level.ToString());
            string fileName = $"os_{x}_{y}_{level}.png";
            string filePath = Path.Combine(tileDir, fileName);

            if (File.Exists(filePath))
            {
                // Uri를 사용할 때 메모리 누수를 방지하고 파일을 즉시 로드하도록 옵션을 설정합니다.
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad; // 이미지를 즉시 메모리에 로드
                image.UriSource = new Uri(filePath, UriKind.Absolute);
                image.EndInit();
                return image;
            }
            return EmptyTile;
        }

        public override string Name => "LocalTileSource";

        protected override MapDependencyObject CreateObject()
        {
            return new LocalTileSource();
        }
    }
}