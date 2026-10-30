import 'package:dio/dio.dart';

import '../config/api_config.dart';
import '../models/app_user.dart';
import 'api_client.dart';
import 'token_storage.dart';

class AuthService {
  AuthService(this._client, this._storage);

  final ApiClient _client;
  final TokenStorage _storage;

  Future<AppUser> login(String email, String password) async {
    final response = await _client.dio.post<Map<String, dynamic>>(
      '/api/auth/login',
      data: {'email': email, 'password': password},
    );
    return _saveSession(response.data!);
  }

  Future<AppUser> register(String fullName, String email, String password) async {
    final response = await _client.dio.post<Map<String, dynamic>>(
      '/api/auth/register',
      data: {'fullName': fullName, 'email': email, 'password': password},
    );
    return _saveSession(response.data!);
  }

  /// Kayıtlı token varsa API'ye sorarak kullanıcı bilgilerini getirir.
  /// Token yoksa ya da geçersizse null döner.
  Future<AppUser?> currentUser() async {
    final token = await _storage.readToken();
    if (token == null) return null;

    try {
      final response = await _client.dio.get<Map<String, dynamic>>('/api/auth/me');
      return AppUser.fromJson(response.data!);
    } on DioException {
      await _storage.deleteToken();
      return null;
    }
  }

  Future<void> logout() => _storage.deleteToken();

  Future<AppUser> _saveSession(Map<String, dynamic> json) async {
    await _storage.saveToken(json['token'] as String);
    return AppUser.fromJson(json['user'] as Map<String, dynamic>);
  }

  /// API'den gelen hatayı kullanıcıya gösterilecek Türkçe mesaja çevirir.
  static String errorMessage(Object error) {
    if (error is DioException) {
      final data = error.response?.data;
      final status = error.response?.statusCode;

      if (status == 401) {
        if (data is Map && data['message'] is String) return data['message'] as String;
        return 'E-posta veya şifre hatalı.';
      }
      if (status == 400 && data is Map && data['errors'] is Map) {
        // ASP.NET Core doğrulama hataları: { "errors": { "Email": ["..."], ... } }
        final messages = (data['errors'] as Map)
            .values
            .expand((value) => value is List ? value : [value])
            .map((value) => value.toString())
            .toSet();
        return messages.join('\n');
      }
      if (error.type == DioExceptionType.connectionError ||
          error.type == DioExceptionType.connectionTimeout) {
        return 'Sunucuya bağlanılamadı.\nWeb API çalışıyor mu? (${ApiConfig.baseUrl})';
      }
      return 'Sunucu hatası: ${status ?? error.message}';
    }
    return 'Beklenmeyen bir hata oluştu.';
  }
}
