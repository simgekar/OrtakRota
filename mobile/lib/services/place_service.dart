import 'package:dio/dio.dart';

import '../models/place.dart';

class PlaceService {
  /// Dio, ApiClient'tan gelir; böylece token her isteğe otomatik eklenir.
  PlaceService(this._dio);

  final Dio _dio;

  /// Fikir havuzundaki mekanlar (en çok oy alan en üstte). category verilirse filtrelenir.
  Future<List<Place>> getPlaces(int tripId, {String? category}) async {
    final response = await _dio.get<List<dynamic>>(
      '/api/trips/$tripId/places',
      queryParameters: category == null ? null : {'category': category},
    );
    return response.data!
        .map((json) => Place.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  Future<Place> getPlace(int id) async {
    final response = await _dio.get<Map<String, dynamic>>('/api/places/$id');
    return Place.fromJson(response.data!);
  }

  Future<Place> createPlace(int tripId, PlaceInput input) async {
    final response = await _dio.post<Map<String, dynamic>>(
      '/api/trips/$tripId/places',
      data: input.toJson(),
    );
    return Place.fromJson(response.data!);
  }

  Future<void> updatePlace(int id, PlaceInput input) async {
    await _dio.put<void>('/api/places/$id', data: input.toJson());
  }

  Future<void> deletePlace(int id) async {
    await _dio.delete<void>('/api/places/$id');
  }

  /// +1 ya da -1 oy verir; güncel mekan bilgisini (puan, oylar) döner.
  Future<Place> vote(int id, int value) async {
    final response =
        await _dio.put<Map<String, dynamic>>('/api/places/$id/vote', data: {'value': value});
    return Place.fromJson(response.data!);
  }

  /// Oyu geri alır; güncel mekan bilgisini döner.
  Future<Place> removeVote(int id) async {
    final response = await _dio.delete<Map<String, dynamic>>('/api/places/$id/vote');
    return Place.fromJson(response.data!);
  }

  /// Aynı oya tekrar basılırsa oy geri alınır, farklıysa oy verilir/değiştirilir.
  Future<Place> toggleVote(Place place, int value) =>
      place.myVote == value ? removeVote(place.id) : vote(place.id, value);
}
