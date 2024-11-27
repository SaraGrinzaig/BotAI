using Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.interfaces
{
    public interface IWebhookService
    {
        Task SendDataToWebhook(UserDetails userDetails);

    }
}
