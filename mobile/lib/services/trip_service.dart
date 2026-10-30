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

  /// Hata nesnesini kullanıcıya gösterilecek Türkçe bir mesaja çevirir.
  static String errorMessage(Object error) {
    if (error is DioException) {
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
