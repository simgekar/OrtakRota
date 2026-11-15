import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

import '../models/place.dart';

/// Haritada başlangıç noktası: hiç mekan yoksa Türkiye'nin ortası
const turkeyCenter = LatLng(39.0, 35.0);

/// OpenStreetMap harita karoları
TileLayer osmTileLayer() => TileLayer(
      urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
      userAgentPackageName: 'com.ortakrota.ortakrota_mobile',
    );

/// OpenStreetMap kullanım koşulları gereği haritada kaynak gösterilir
Widget osmAttribution() => const RichAttributionWidget(
      attributions: [TextSourceAttribution('OpenStreetMap katkıda bulunanlar')],
    );

/// Kategoriye göre renklendirilmiş harita işareti
Marker placeMarker(Place place, {VoidCallback? onTap}) {
  final info = place.categoryInfo;
  return Marker(
    point: LatLng(place.latitude, place.longitude),
    width: 44,
    height: 44,
    alignment: Alignment.topCenter,
    child: GestureDetector(
      onTap: onTap,
      child: Icon(Icons.location_on, size: 44, color: info.color),
    ),
  );
}
