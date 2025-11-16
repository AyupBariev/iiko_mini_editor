using System;
using System.Linq;
using System.Net;
using Resto.Front.Api.V8;
using Resto.Front.Api.V8.Data.Orders;
using Resto.Front.Api.V8.Data.Security;
using Resto.Front.Api.V8.Editors;

namespace IikoMiniEditor
{
    internal class Program
    {
        private static void Main()
        {
            // 1. Подключаемся к iikoFront (должен быть уже запущен)
            var api = PluginContext.Operations;
            Console.WriteLine("Connected to iikoFront");

            // 2. Авторизуемся под первым найденным менеджером
            //    В проде лучше брать конкретный credentials из конфига
            ICredentials credentials = api.GetCredentialsByUserName("manager"); // имя пользователя
            if (credentials == null)
            {
                Console.WriteLine("Manager credentials not found");
                Console.ReadKey();
                return;
            }

            // 3. Берём первый неоплаченный заказ (или любой другой)
            IOrder order = api.GetOrders()
                              .Where(o => o.Status == OrderStatus.New)
                              .OrderByDescending(o => o.CreationTime)
                              .FirstOrDefault();

            if (order == null)
            {
                Console.WriteLine("No open order found");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"Working with order #{order.Number}  table:{order.Tables.FirstOrDefault()?.Number}");

            // 4. Создаём сессию редактирования
            using (IEditSession edit = api.CreateEditSession())
            {
                // 4.1 Добавляем блюдо
                var product = api.GetAllProducts()
                                 .FirstOrDefault(p => p.Name.ToLower().Contains("капучино"));
                if (product != null)
                {
                    edit.AddOrderProduct(order, product, 1m, null, null);
                    Console.WriteLine($"Added product: {product.Name}");
                }

                // 4.2 Меняем комментарий
                string newComment = $"Changed by console tool at {DateTime.Now:G}";
                edit.ChangeOrderComment(order, newComment);
                Console.WriteLine("Comment updated");

                // 4.3 Кладём внешние данные
                edit.AddOrderExternalData(order, "ConsoleTool", "order patched", isPublic: true);
                Console.WriteLine("External data added");

                // 5. Коммитим
                api.SubmitChanges(credentials, edit);
                Console.WriteLine("Changes submitted successfully");
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}