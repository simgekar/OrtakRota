import 'package:dio/dio.dart';

import '../config/api_config.dart';
import '../models/trip.dart';

class TripService {
  TripService({Dio? dio})
      : _dio = dio ??
            Dio(
              BaseOptions(
                baseUrl: ApiConfig.baseUrl,
                connectTimeout: const Duration(seconds: 10),
                receiveTimeout: const Duration(seconds: 10),
              ),
            );

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
      if (error.response?.statusCode == 404) {
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
