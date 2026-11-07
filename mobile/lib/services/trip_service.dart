import 'package:dio/dio.dart';

import '../config/api_config.dart';
import '../models/trip.dart';

class TripService {
  /// Dio, ApiClient'tan gelir; böylece token her isteğe otomatik eklenir.
  TripService(this._dio);

  final Dio _dio;

  Future<List<Trip>> getTrips() async {
    final response = await _dio.get<List<dynamic>>('/api/trips');
    return response.data!
        .map((json) => Trip.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  Future<Trip> getTrip(int id) async {
    final response = await _dio.get<Map<String, dynamic>>('/api/trips/$id');
    return Trip.fromJson(response.data!);
  }

  Future<Trip> createTrip(TripInput input) async {
    final response =
        await _dio.post<Map<String, dynamic>>('/api/trips', data: input.toJson());
    return Trip.fromJson(response.data!);
  }

  Future<void> updateTrip(int id, TripInput input) async {
    await _dio.put<void>('/api/trips/$id', data: input.toJson());
  }

  Future<void> deleteTrip(int id) async {
    await _dio.delete<void>('/api/trips/$id');
  }

  /// Davet koduyla geziye katılır; katılınan gezinin bilgilerini döner.
  Future<Trip> joinTrip(String inviteCode) async {
    final response = await _dio.post<Map<String, dynamic>>(
      '/api/trips/join',
      data: {'inviteCode': inviteCode},
    );
    return Trip.fromJson(response.data!);
  }

  Future<String> regenerateInviteCode(int tripId) async {
    final response =
        await _dio.post<Map<String, dynamic>>('/api/trips/$tripId/invite-code');
    return response.data!['inviteCode'] as String;
  }

  Future<void> updateMemberRole(int tripId, String userId, String role) async {
    await _dio.put<void>('/api/trips/$tripId/members/$userId', data: {'role': role});
  }

  /// Başka bir üye için çağrılırsa üyeyi çıkarır, kendi kimliğiyle çağrılırsa geziden ayrılır.
  Future<void> removeMember(int tripId, String userId) async {
    await _dio.delete<void>('/api/trips/$tripId/members/$userId');
  }

  /// Hata nesnesini kullanıcıya gösterilecek Türkçe bir mesaja çevirir.
  static String errorMessage(Object error) {
    if (error is DioException) {
      final data = error.response?.data;

      // API'nin kendi mesajı varsa onu göster ({ "message": "..." })
      if (data is Map && data['message'] is String) {
        return data['message'] as String;
      }
      // Doğrulama hataları ({ "errors": { "Name": ["..."] } })
      if (data is Map && data['errors'] is Map) {
        return (data['errors'] as Map)
            .values
            .expand((value) => value is List ? value : [value])
            .map((value) => value.toString())
            .toSet()
            .join('\n');
      }

      switch (error.response?.statusCode) {
        case 401:
          return 'Oturumun sona erdi, lütfen tekrar giriş yap.';
        case 403:
          return 'Bu işlem için yetkin yok.';
        case 404:
          return 'Gezi bulunamadı.';
      }
      if (error.type == DioExceptionType.connectionError ||
          error.type == DioExceptionType.connectionTimeout) {
        return 'Sunucuya bağlanılamadı.\nWeb API çalışıyor mu? (${ApiConfig.baseUrl})';
      }
      return 'Sunucu hatası: ${error.response?.statusCode ?? error.message}';
    }
    return 'Beklenmeyen bir hata oluştu.';
  }
}
