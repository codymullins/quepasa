namespace Quepasa.Web.Client.Models;

public class StatusChecker(HttpClient http, StatusResponseTransformer responseTransformer)
{
    public async ValueTask<StatusInfo> CheckStatusAsync(string url, CheckType checkType = default)
    {
        try
        {
            if (checkType == CheckType.StatusCode)
            {
                var response = await http.GetAsync(url);
                return new()
                {
                    RawStatus = response.IsSuccessStatusCode ? ServiceStatus.Operational.ToString() : ServiceStatus.MajorOutage.ToString(),
                    LastChecked = DateTimeOffset.Now
                };
            }
            
            var json = await http.GetStringAsync(url);
            var status = responseTransformer.TransformStatus(json);
            return status;
        }
        catch (Exception e)
        {
            return new()
            {
                // todo: depending on the exception, we could return a more specific status
                RawStatus = ServiceStatus.Unknown.ToString(),
                LastChecked = DateTime.Now
            };
        }
    }
}