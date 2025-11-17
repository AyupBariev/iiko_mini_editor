using System;
using System.Linq;
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
            var api = PluginContext.Operations;
            Console.WriteLine("Connected to iikoFront");

            // Получаем менеджера
            ICredentials credentials = api.GetCredentialsByUserName("manager");
            if (credentials == null)
            {
                Console.WriteLine("Manager credentials not found");
                return;
            }

            while (true)
            {
                Console.WriteLine("\nChoose action:");
                Console.WriteLine("1 - List new orders");
                Console.WriteLine("2 - Edit an order");
                Console.WriteLine("0 - Exit");
                Console.Write("Your choice: ");
                var input = Console.ReadLine();

                if (input == "0") break;

                switch (input)
                {
                    case "1":
                        ListOrders(api);
                        break;
                    case "2":
                        EditOrder(api, credentials);
                        break;
                    default:
                        Console.WriteLine("Invalid choice");
                        break;
                }
            }
        }

        private static void ListOrders(IOperations api)
        {
            var orders = api.GetOrders()
                            .Where(o => o.Status == OrderStatus.New)
                            .OrderByDescending(o => o.CreationTime)
                            .ToList();

            if (!orders.Any())
            {
                Console.WriteLine("No new orders found");
                return;
            }

            Console.WriteLine("New orders:");
            foreach (var o in orders)
            {
                Console.WriteLine($"#{o.Number} Table: {o.Tables.FirstOrDefault()?.Number} Time: {o.CreationTime}");
            }
        }

        private static void EditOrder(IOperations api, ICredentials credentials)
        {
            Console.Write("Enter order number to edit: ");
            var orderNumberStr = Console.ReadLine();

            if (!int.TryParse(orderNumberStr, out int orderNumber))
            {
                Console.WriteLine("Invalid number");
                return;
            }

            var order = api.GetOrders()
                           .FirstOrDefault(o => o.Number == orderNumber);

            if (order == null)
            {
                Console.WriteLine("Order not found");
                return;
            }

            Console.WriteLine($"Editing order #{order.Number} Table:{order.Tables.FirstOrDefault()?.Number}");

            using (IEditSession edit = api.CreateEditSession())
            {
                // Добавляем продукт
                var product = api.GetAllProducts()
                                 .FirstOrDefault(p => p.Name.ToLower().Contains("наггетсы"));
                if (product != null)
                {
                    edit.AddOrderProduct(order, product, 1m, null, null);
                    Console.WriteLine($"Added product: {product.Name}");
                }

                // Меняем комментарий
                string newComment = $"Changed by console tool at {DateTime.Now:G}";
                edit.ChangeOrderComment(order, newComment);
                Console.WriteLine("Comment updated");

                // Добавляем внешние данные
                edit.AddOrderExternalData(order, "ConsoleTool", "order patched", isPublic: true);
                Console.WriteLine("External data added");

                // Сохраняем изменения
                api.SubmitChanges(credentials, edit);
                Console.WriteLine("Changes submitted successfully");
            }
        }
    }
}
