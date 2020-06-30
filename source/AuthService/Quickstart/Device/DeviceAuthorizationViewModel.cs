namespace IdentityServerHost.Quickstart.UI
{
    public class DeviceAuthorizationViewModel : ConsentViewModel
    {
        public bool ConfirmUserCode { get; set; }

        public string UserCode { get; set; }
    }
}
