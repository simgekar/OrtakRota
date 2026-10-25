/// Web API adresi.
///
/// Android emülatörü, bilgisayardaki localhost'a 10.0.2.2 adresiyle ulaşır.
/// Gerçek cihazda çalıştırırken bilgisayarın yerel IP adresi verilir:
///   flutter run --dart-define=API_URL=http://192.168.1.20:5180
class ApiConfig {
  static const String baseUrl = String.fromEnvironment(
    'API_URL',
    defaultValue: 'http://10.0.2.2:5180',
  );
}
