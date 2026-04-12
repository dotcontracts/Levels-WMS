using Microsoft.Maui.Controls;

namespace wms_android.Controls
{
    public partial class SkeletonLoading : ContentView
    {
        public SkeletonLoading()
        {
            InitializeComponent();
        StartShimmerAnimation();
        }

        private void StartShimmerAnimation()
        {
            // Simple shimmer animation could be added here
            // For now, static loading effect
        }
    }
}