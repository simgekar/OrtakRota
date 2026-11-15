import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:go_router/go_router.dart';
import 'package:latlong2/latlong.dart';
import 'package:provider/provider.dart';

import '../models/place.dart';
import '../services/place_service.dart';
import '../services/trip_service.dart';
import '../utils/validators.dart';
import '../widgets/osm_map.dart';

/// Mekan önerme ve düzenleme formu. Konum, haritaya dokunarak seçilir.
class PlaceFormScreen extends StatefulWidget {
  const PlaceFormScreen({super.key, required this.tripId, this.placeId});

  final int tripId;
  final int? placeId;

  bool get isEdit => placeId != null;

  @override
  State<PlaceFormScreen> createState() => _PlaceFormScreenState();
}

class _PlaceFormScreenState extends State<PlaceFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _addressController = TextEditingController();
  final _noteController = TextEditingController();
  final _mapController = MapController();

  String _category = PlaceCategoryInfo.all.first.value;
  LatLng? _location;
  LatLng _mapCenter = turkeyCenter;
  double _mapZoom = 5.5;
  String? _locationError;
  bool _isLoading = true;
  bool _isSaving = false;

  @override
  void initState() {
    super.initState();
    // Yükleme ekran ilk kez çizildikten sonra başlar; initState içinde
    // ScaffoldMessenger gibi üst widget'lara erişilemez
    WidgetsBinding.instance.addPostFrameCallback((_) => _prepare());
  }

  @override
  void dispose() {
    _nameController.dispose();
    _addressController.dispose();
    _noteController.dispose();
    super.dispose();
  }

  /// Düzenlemede mevcut mekan yüklenir; yeni mekanda harita, gezideki ilk mekanın çevresinde açılır.
  Future<void> _prepare() async {
    final service = context.read<PlaceService>();
    final messenger = ScaffoldMessenger.of(context);
    try {
      if (widget.isEdit) {
        final place = await service.getPlace(widget.placeId!);
        _nameController.text = place.name;
        _addressController.text = place.address ?? '';
        _noteController.text = place.note ?? '';
        _category = place.category;
        _location = LatLng(place.latitude, place.longitude);
        _mapCenter = _location!;
        _mapZoom = 14;
      } else {
        final places = await service.getPlaces(widget.tripId);
        if (places.isNotEmpty) {
          _mapCenter = LatLng(places.first.latitude, places.first.longitude);
          _mapZoom = 12;
        }
      }
    } catch (error) {
      messenger.showSnackBar(SnackBar(content: Text(TripService.errorMessage(error))));
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _save() async {
    final formValid = _formKey.currentState!.validate();
    final locationError = _location == null ? 'Haritaya dokunarak mekanın konumunu seç.' : null;
    setState(() => _locationError = locationError);
    if (!formValid || locationError != null) return;

    final service = context.read<PlaceService>();
    final messenger = ScaffoldMessenger.of(context);
    final router = GoRouter.of(context);
    final address = _addressController.text.trim();
    final note = _noteController.text.trim();
    final input = PlaceInput(
      name: _nameController.text.trim(),
      category: _category,
      latitude: _location!.latitude,
      longitude: _location!.longitude,
      address: address.isEmpty ? null : address,
      note: note.isEmpty ? null : note,
    );

    setState(() => _isSaving = true);
    try {
      if (widget.isEdit) {
        await service.updatePlace(widget.placeId!, input);
        messenger.showSnackBar(const SnackBar(content: Text('Mekan güncellendi.')));
      } else {
        await service.createPlace(widget.tripId, input);
        messenger.showSnackBar(const SnackBar(content: Text('Mekan fikir havuzuna eklendi.')));
      }
      router.pop(true);
    } catch (error) {
      messenger.showSnackBar(SnackBar(content: Text(TripService.errorMessage(error))));
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(title: Text(widget.isEdit ? 'Mekanı Düzenle' : 'Mekan Öner')),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : SafeArea(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(16),
                child: Form(
                  key: _formKey,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      TextFormField(
                        controller: _nameController,
                        textCapitalization: TextCapitalization.words,
                        decoration: const InputDecoration(
                          labelText: 'Mekan adı',
                          prefixIcon: Icon(Icons.place_outlined),
                        ),
                        validator: Validators.placeName,
                      ),
                      const SizedBox(height: 16),
                      DropdownButtonFormField<String>(
                        initialValue: _category,
                        decoration: const InputDecoration(labelText: 'Kategori'),
                        items: [
                          for (final c in PlaceCategoryInfo.all)
                            DropdownMenuItem(
                              value: c.value,
                              child: Row(
                                children: [
                                  Icon(c.icon, color: c.color, size: 20),
                                  const SizedBox(width: 8),
                                  Text(c.label),
                                ],
                              ),
                            ),
                        ],
                        onChanged: (value) => setState(() => _category = value ?? _category),
                      ),
                      const SizedBox(height: 16),
                      Text('Konum', style: theme.textTheme.titleSmall),
                      const SizedBox(height: 4),
                      Text(
                        _location == null
                            ? 'Haritada mekanın olduğu yere dokun.'
                            : 'Seçilen konum: ${_location!.latitude.toStringAsFixed(5)}, '
                                '${_location!.longitude.toStringAsFixed(5)}',
                        style: theme.textTheme.bodySmall,
                      ),
                      const SizedBox(height: 8),
                      ClipRRect(
                        borderRadius: BorderRadius.circular(12),
                        child: SizedBox(
                          height: 280,
                          child: FlutterMap(
                            mapController: _mapController,
                            options: MapOptions(
                              initialCenter: _mapCenter,
                              initialZoom: _mapZoom,
                              onTap: (_, point) => setState(() {
                                _location = point;
                                _locationError = null;
                              }),
                            ),
                            children: [
                              osmTileLayer(),
                              if (_location != null)
                                MarkerLayer(
                                  markers: [
                                    Marker(
                                      point: _location!,
                                      width: 44,
                                      height: 44,
                                      alignment: Alignment.topCenter,
                                      child: Icon(
                                        Icons.location_on,
                                        size: 44,
                                        color: PlaceCategoryInfo.of(_category).color,
                                      ),
                                    ),
                                  ],
                                ),
                              osmAttribution(),
                            ],
                          ),
                        ),
                      ),
                      if (_locationError != null) ...[
                        const SizedBox(height: 8),
                        Text(_locationError!, style: TextStyle(color: theme.colorScheme.error)),
                      ],
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _addressController,
                        decoration: const InputDecoration(
                          labelText: 'Adres (isteğe bağlı)',
                          prefixIcon: Icon(Icons.home_outlined),
                        ),
                        validator: Validators.placeAddress,
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _noteController,
                        maxLines: 3,
                        maxLength: 500,
                        decoration: const InputDecoration(
                          labelText: 'Not (isteğe bağlı)',
                          hintText: 'Neden gitmeliyiz? Fiyat, saat, öneri...',
                          alignLabelWithHint: true,
                        ),
                        validator: Validators.placeNote,
                      ),
                      const SizedBox(height: 16),
                      FilledButton.icon(
                        onPressed: _isSaving ? null : _save,
                        style: FilledButton.styleFrom(
                          padding: const EdgeInsets.symmetric(vertical: 16),
                        ),
                        icon: _isSaving
                            ? const SizedBox(
                                height: 20,
                                width: 20,
                                child: CircularProgressIndicator(strokeWidth: 2),
                              )
                            : const Icon(Icons.check),
                        label: Text(widget.isEdit ? 'Kaydet' : 'Fikir Havuzuna Ekle'),
                      ),
                    ],
                  ),
                ),
              ),
            ),
    );
  }
}
