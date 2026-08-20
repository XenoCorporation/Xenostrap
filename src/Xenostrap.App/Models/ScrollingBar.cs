namespace Xenostrap.Helpers
{
    public static class SmoothScrollBehavior
    {
        static SmoothScrollBehavior()
        {
            Wpf.Ui.Controls.SmoothScroll.Register();
        }
    }
}
