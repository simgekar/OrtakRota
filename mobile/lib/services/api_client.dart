import 'package:dio/dio.dart';

import '../config/api_config.dart';
import 'token_storage.dart';

/// Tüm API istekleri bu ortak Dio nesnesi üzerinden yapılır.
/// Interceptor, kayıtlı token'ı her isteğe "Authorization: Bearer ..." olarak ekler.
class ApiClient {
  ApiClient(this._storage)
      : dio = Dio(
          BaseOptions(
            baseUrl: ApiConfig.baseUrl,
            connectTimeout: const Duration(seconds: 10),
            receiveTimeout: const Duration(seconds: 10),
          ),
        ) {
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          final token = await _storage.readToken();
          if (token != null) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          handler.next(options);
        },
        onError: (error, handler) {
          final isAuthRequest = error.requestOptions.path.startsWith('/api/auth');
          if (error.response?.statusCode == 401 && !isAuthRequest) {
            onUnauthorized?.call();
          }
          handler.next(error);
        },
      ),
    );
  }

  final TokenStorage _storage;
  final Dio dio;

  /// Token geçersiz olduğunda çağrılır (main.dart'ta çıkış işlemine bağlanır).
  void Function()? onUnauthorized;
}
