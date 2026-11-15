import 'package:flutter/material.dart';

/// Mekan kategorileri: API'deki adı, Türkçe etiketi, simgesi ve harita rengi
class PlaceCategoryInfo {
  final String value;
  final String label;
  final IconData icon;
  final Color color;

  const PlaceCategoryInfo(this.value, this.label, this.icon, this.color);

  static const all = [
    PlaceCategoryInfo('Sight', 'Gezilecek Yer', Icons.photo_camera_outlined, Colors.teal),
    PlaceCategoryInfo('Food', 'Restoran / Kafe', Icons.restaurant, Colors.orange),
    PlaceCategoryInfo('Museum', 'Müze', Icons.museum_outlined, Colors.indigo),
    PlaceCategoryInfo('Accommodation', 'Konaklama', Icons.hotel_outlined, Colors.purple),
    PlaceCategoryInfo('Shopping', 'Alışveriş', Icons.shopping_bag_outlined, Colors.pink),
    PlaceCategoryInfo('Other', 'Diğer', Icons.place_outlined, Colors.blueGrey),
  ];

  static PlaceCategoryInfo of(String value) =>
      all.firstWhere((c) => c.value == value, orElse: () => all.last);
}

class Place {
  final int id;
  final int tripId;
  final String name;
  final String category;
  final double latitude;
  final double longitude;
  final String? address;
  final String? note;
  final String addedById;
  final String addedByName;
  final int score;
  final int upVotes;
  final int downVotes;

  /// Giriş yapan kullanıcının oyu: 1, -1 ya da 0 (oy vermedi)
  final int myVote;

  /// Giriş yapan kullanıcı bu mekanı düzenleyip silebilir mi?
  final bool canEdit;

  const Place({
    required this.id,
    required this.tripId,
    required this.name,
    required this.category,
    required this.latitude,
    required this.longitude,
    this.address,
    this.note,
    required this.addedById,
    required this.addedByName,
    required this.score,
    required this.upVotes,
    required this.downVotes,
    required this.myVote,
    required this.canEdit,
  });

  factory Place.fromJson(Map<String, dynamic> json) {
    return Place(
      id: json['id'] as int,
      tripId: json['tripId'] as int,
      name: json['name'] as String,
      category: json['category'] as String,
      latitude: (json['latitude'] as num).toDouble(),
      longitude: (json['longitude'] as num).toDouble(),
      address: json['address'] as String?,
      note: json['note'] as String?,
      addedById: json['addedById'] as String? ?? '',
      addedByName: json['addedByName'] as String? ?? '',
      score: json['score'] as int? ?? 0,
      upVotes: json['upVotes'] as int? ?? 0,
      downVotes: json['downVotes'] as int? ?? 0,
      myVote: json['myVote'] as int? ?? 0,
      canEdit: json['canEdit'] as bool? ?? false,
    );
  }

  PlaceCategoryInfo get categoryInfo => PlaceCategoryInfo.of(category);
}

/// Mekan ekleme/güncelleme formundan API'ye gönderilen veri
class PlaceInput {
  final String name;
  final String category;
  final double latitude;
  final double longitude;
  final String? address;
  final String? note;

  const PlaceInput({
    required this.name,
    required this.category,
    required this.latitude,
    required this.longitude,
    this.address,
    this.note,
  });

  Map<String, dynamic> toJson() => {
        'name': name,
        'category': category,
        'latitude': latitude,
        'longitude': longitude,
        'address': address,
        'note': note,
      };
}
