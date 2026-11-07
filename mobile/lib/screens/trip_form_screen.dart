import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../models/trip.dart';
import '../services/trip_service.dart';
import '../utils/validators.dart';

/// Gezi oluşturma ve düzenleme formu. tripId verilirse düzenleme modunda açılır.
class TripFormScreen extends StatefulWidget {
  const TripFormScreen({super.key, this.tripId});

  final int? tripId;

  bool get isEdit => tripId != null;

  @override
  State<TripFormScreen> createState() => _TripFormScreenState();
}

class _TripFormScreenState extends State<TripFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _descriptionController = TextEditingController();
  final _dateFormat = DateFormat('dd.MM.yyyy');

  DateTime? _startDate;
  DateTime? _endDate;
  String? _dateError;
  bool _isLoading = false;
  bool _isSaving = false;

  @override
  void initState() {
    super.initState();
    if (widget.isEdit) _loadTrip();
  }

  @override
  void dispose() {
    _nameController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  // Düzenleme modunda mevcut gezi bilgileri forma doldurulur
  Future<void> _loadTrip() async {
    final service = context.read<TripService>();
    final messenger = ScaffoldMessenger.of(context);
    setState(() => _isLoading = true);
    try {
      final trip = await service.getTrip(widget.tripId!);
      _nameController.text = trip.name;
      _descriptionController.text = trip.description ?? '';
      _startDate = trip.startDate;
      _endDate = trip.endDate;
    } catch (error) {
      messenger.showSnackBar(SnackBar(content: Text(TripService.errorMessage(error))));
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _pickDate({required bool isStart}) async {
    final now = DateTime.now();
    final initial = (isStart ? _startDate : _endDate) ?? _startDate ?? now;
    final picked = await showDatePicker(
      context: context,
      initialDate: initial,
      firstDate: DateTime(now.year - 1),
      lastDate: DateTime(now.year + 5),
      helpText: isStart ? 'Başlangıç tarihi' : 'Bitiş tarihi',
    );
    if (picked == null) return;

    setState(() {
      if (isStart) {
        _startDate = picked;
        // Bitiş tarihi yeni başlangıçtan önce kalırsa aynı güne çekilir
        if (_endDate != null && _endDate!.isBefore(picked)) _endDate = picked;
      } else {
        _endDate = picked;
      }
      _dateError = null;
    });
  }

  Future<void> _save() async {
    final formValid = _formKey.currentState!.validate();
    final dateError = Validators.tripDates(_startDate, _endDate);
    setState(() => _dateError = dateError);
    if (!formValid || dateError != null) return;

    final service = context.read<TripService>();
    final messenger = ScaffoldMessenger.of(context);
    final router = GoRouter.of(context);
    final description = _descriptionController.text.trim();
    final input = TripInput(
      name: _nameController.text.trim(),
      description: description.isEmpty ? null : description,
      startDate: _startDate!,
      endDate: _endDate!,
    );

    setState(() => _isSaving = true);
    try {
      if (widget.isEdit) {
        await service.updateTrip(widget.tripId!, input);
        messenger.showSnackBar(const SnackBar(content: Text('Gezi güncellendi.')));
      } else {
        await service.createTrip(input);
        messenger.showSnackBar(const SnackBar(content: Text('Gezi oluşturuldu.')));
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
    return Scaffold(
      appBar: AppBar(title: Text(widget.isEdit ? 'Geziyi Düzenle' : 'Yeni Gezi')),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : SafeArea(
              child: Center(
                child: SingleChildScrollView(
                  padding: const EdgeInsets.all(24),
                  child: ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: 520),
                    child: Form(
                      key: _formKey,
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          TextFormField(
                            controller: _nameController,
                            textCapitalization: TextCapitalization.sentences,
                            decoration: const InputDecoration(
                              labelText: 'Gezi adı',
                              prefixIcon: Icon(Icons.flag_outlined),
                            ),
                            validator: Validators.tripName,
                          ),
                          const SizedBox(height: 16),
                          TextFormField(
                            controller: _descriptionController,
                            maxLines: 3,
                            maxLength: 500,
                            decoration: const InputDecoration(
                              labelText: 'Açıklama (isteğe bağlı)',
                              alignLabelWithHint: true,
                            ),
                            validator: Validators.tripDescription,
                          ),
                          const SizedBox(height: 8),
                          Row(
                            children: [
                              Expanded(
                                child: _DateField(
                                  label: 'Başlangıç',
                                  value: _startDate == null ? null : _dateFormat.format(_startDate!),
                                  onTap: () => _pickDate(isStart: true),
                                ),
                              ),
                              const SizedBox(width: 12),
                              Expanded(
                                child: _DateField(
                                  label: 'Bitiş',
                                  value: _endDate == null ? null : _dateFormat.format(_endDate!),
                                  onTap: () => _pickDate(isStart: false),
                                ),
                              ),
                            ],
                          ),
                          if (_dateError != null) ...[
                            const SizedBox(height: 8),
                            Text(
                              _dateError!,
                              style: TextStyle(color: Theme.of(context).colorScheme.error),
                            ),
                          ],
                          const SizedBox(height: 24),
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
                            label: Text(widget.isEdit ? 'Kaydet' : 'Geziyi Oluştur'),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ),
            ),
    );
  }
}

class _DateField extends StatelessWidget {
  const _DateField({required this.label, required this.value, required this.onTap});

  final String label;
  final String? value;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(4),
      child: InputDecorator(
        decoration: InputDecoration(
          labelText: label,
          prefixIcon: const Icon(Icons.calendar_today_outlined),
        ),
        child: Text(value ?? 'Seç'),
      ),
    );
  }
}
