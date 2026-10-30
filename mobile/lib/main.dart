import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import 'app_router.dart';
import 'services/api_client.dart';
import 'services/auth_service.dart';
import 'services/token_storage.dart';
import 'services/trip_service.dart';
import 'state/auth_provider.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();

  final storage = TokenStorage();
  final apiClient = ApiClient(storage);
  final authProvider = AuthProvider(AuthService(apiClient, storage));
  final tripService = TripService(apiClient.dio);

  // Token süresi dolarsa (API 401 dönerse) kullanıcı otomatik çıkış yapar
  apiClient.onUnauthorized = authProvider.logout;

  // Uygulama açılırken kayıtlı token varsa oturum geri yüklenir
  authProvider.init();

  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider.value(value: authProvider),
        Provider.value(value: tripService),
      ],
      child: OrtakRotaApp(router: createRouter(authProvider)),
    ),
  );
}

class OrtakRotaApp extends StatelessWidget {
  const OrtakRotaApp({super.key, required this.router});

  final GoRouter router;

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      title: 'OrtakRota',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorSchemeSeed: const Color(0xFF0F766E),
        useMaterial3: true,
        inputDecorationTheme: const InputDecorationTheme(
          border: OutlineInputBorder(),
        ),
      ),
      routerConfig: router,
    );
  }
}
