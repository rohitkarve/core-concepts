using System;
using System.Collections.Generic;
using System.Timers;

namespace MemoryLeakExamples
{
    // ============================================
    // EXAMPLE 1: Event Handler Memory Leak
    // ============================================
    
    // Publisher class that raises events
    public class DataPublisher
    {
        public event EventHandler<string> DataReceived;
        
        public void PublishData(string data)
        {
            DataReceived?.Invoke(this, data);
        }
    }
    
    // Subscriber that causes memory leak if not unsubscribed
    public class DataSubscriber
    {
        private readonly string _name;
        private readonly byte[] _largeData = new byte[10_000_000]; // 10MB to simulate memory usage
        
        public DataSubscriber(string name, DataPublisher publisher)
        {
            _name = name;
            // MEMORY LEAK: Subscribing to event creates a strong reference
            // Publisher holds reference to subscriber, preventing garbage collection
            publisher.DataReceived += OnDataReceived;
        }
        
        private void OnDataReceived(object sender, string data)
        {
            Console.WriteLine($"{_name} received: {data}");
        }
        
        // FIX: Implement IDisposable and unsubscribe
        // public void Dispose()
        // {
        //     _publisher.DataReceived -= OnDataReceived;
        // }
    }
    
    // ============================================
    // EXAMPLE 2: Static Collection Memory Leak
    // ============================================
    
    public class CacheManager
    {
        // MEMORY LEAK: Static collections never get garbage collected
        // Objects added here stay in memory for the lifetime of the application
        private static readonly List<byte[]> _cache = new List<byte[]>();
        
        public static void AddToCache(byte[] data)
        {
            _cache.Add(data); // Objects keep accumulating, never removed
        }
        
        // FIX: Implement cache eviction policy
        // public static void ClearCache()
        // {
        //     _cache.Clear();
        // }
    }
    
    // ============================================
    // EXAMPLE 3: Timer Not Disposed
    // ============================================
    
    public class TimerExample
    {
        private Timer _timer;
        private readonly byte[] _data = new byte[5_000_000]; // 5MB
        
        public void Start()
        {
            _timer = new Timer(1000);
            // MEMORY LEAK: Timer holds reference to this object
            // Even if TimerExample goes out of scope, timer keeps it alive
            _timer.Elapsed += (s, e) => ProcessData();
            _timer.Start();
        }
        
        private void ProcessData()
        {
            Console.WriteLine("Processing...");
        }
        
        // FIX: Implement IDisposable
        // public void Dispose()
        // {
        //     _timer?.Stop();
        //     _timer?.Dispose();
        // }
    }
    
    // ============================================
    // EXAMPLE 4: Closure Capturing Variables
    // ============================================
    
    public class ClosureExample
    {
        public Action CreateLeakyAction()
        {
            // Large object
            var largeObject = new byte[50_000_000]; // 50MB
            
            // MEMORY LEAK: Lambda captures 'largeObject' in closure
            // As long as the returned Action exists, largeObject cannot be GC'd
            return () => Console.WriteLine($"Data size: {largeObject.Length}");
        }
    }
    
    // ============================================
    // REAL LIFE SCENARIO: WPF/WinForms Application
    // ============================================
    
    public class OrderService
    {
        public event EventHandler<decimal> OrderPlaced;
        
        public void PlaceOrder(decimal amount)
        {
            OrderPlaced?.Invoke(this, amount);
        }
    }
    
    // Simulates a UI window that subscribes to a long-lived service
    public class OrderWindow
    {
        private readonly string _windowId;
        private readonly byte[] _windowResources = new byte[20_000_000]; // 20MB UI resources
        
        public OrderWindow(string id, OrderService service)
        {
            _windowId = id;
            // MEMORY LEAK: Window subscribes to service but never unsubscribes
            // When user closes window, it cannot be garbage collected
            // because OrderService still holds a reference through the event
            service.OrderPlaced += OnOrderPlaced;
        }
        
        private void OnOrderPlaced(object sender, decimal amount)
        {
            Console.WriteLine($"Window {_windowId}: Order placed for ${amount}");
        }
    }
    
    // ============================================
    // DEMONSTRATION
    // ============================================
    
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Memory Leak Demonstrations ===\n");
            
            // Example 1: Event Handler Leak
            Console.WriteLine("1. Event Handler Leak:");
            var publisher = new DataPublisher();
            for (int i = 0; i < 5; i++)
            {
                // These subscribers will NEVER be garbage collected
                // because publisher holds references to them
                var subscriber = new DataSubscriber($"Subscriber-{i}", publisher);
                Console.WriteLine($"   Created subscriber {i} (10MB each, never freed)");
            }
            
            GC.Collect();
            Console.WriteLine("   After GC.Collect() - subscribers still in memory!\n");
            
            // Example 2: Static Collection Leak
            Console.WriteLine("2. Static Collection Leak:");
            for (int i = 0; i < 3; i++)
            {
                CacheManager.AddToCache(new byte[10_000_000]); // 10MB each
                Console.WriteLine($"   Added 10MB to static cache (iteration {i})");
            }
            Console.WriteLine("   Cache grows forever, never cleared!\n");
            
            // Real-life scenario
            Console.WriteLine("3. Real-Life UI Scenario:");
            var orderService = new OrderService(); // Long-lived singleton service
            for (int i = 0; i < 3; i++)
            {
                // Simulating user opening and "closing" windows
                var window = new OrderWindow($"Window-{i}", orderService);
                Console.WriteLine($"   User opened Window-{i} (20MB resources)");
                // User closes window, but window object cannot be collected!
            }
            Console.WriteLine("   All windows 'closed' but 60MB still in memory!\n");
            
            Console.WriteLine("=== Solutions ===");
            Console.WriteLine("1. Always unsubscribe from events (implement IDisposable)");
            Console.WriteLine("2. Use WeakEventManager for event subscriptions");
            Console.WriteLine("3. Clear static collections when appropriate");
            Console.WriteLine("4. Dispose timers and other IDisposable objects");
            Console.WriteLine("5. Be careful with closures capturing large objects");
            
            Console.ReadKey();
        }
    }
}