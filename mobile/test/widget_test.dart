import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:ortakrota_mobile/models/place.dart';
import 'package:ortakrota_mobile/models/trip.dart';
import 'package:ortakrota_mobile/screens/login_screen.dart';
import 'package:ortakrota_mobile/screens/register_screen.dart';
import 'package:ortakrota_mobile/screens/trip_form_screen.dart';
import 'package:ortakrota_mobile/services/api_client.dart';
import 'package:ortakrota_mobile/services/auth_service.dart';
import 'package:ortakrota_mobile/services/token_storage.dart';
import 'package:ortakrota_mobile/state/auth_provider.dart';
import 'package:ortakrota_mobile/utils/validators.dart';

Widget _wrap(Widget screen) {
  final storage = TokenStorage();
  final auth = AuthProvider(AuthService(ApiClient(storage), storage));
  return ChangeNotifierProvider.value(
    value: auth,
    child: MaterialApp(home: screen),
  );
}

void main() {
  testWidgets('Giriş formu boş gönderilince doğrulama mesajları görünür',
      (WidgetTester tester) async {
    await tester.pumpWidget(_wrap(const LoginScreen()));

    await tester.tap(find.byType(FilledButton));
    await tester.pump();

    expect(find.text('E-posta zorunludur.'), findsOneWidget);
    expect(find.text('Şifre zorunludur.'), findsOneWidget);
  });

  testWidgets('Kayıt formunda şifreler eşleşmezse uyarı görünür',
      (WidgetTester tester) async {
    await tester.pumpWidget(_wrap(const RegisterScreen()));

    await tester.enterText(find.byType(TextFormField).at(0), 'Simge Test');
    await tester.enterText(find.byType(TextFormField).at(1), 'simge@test.com');
    await tester.enterText(find.byType(TextFormField).at(2), 'Simge123');
    await tester.enterText(find.byType(TextFormField).at(3), 'Baska123');
    await tester.tap(find.byType(FilledButton));
    await tester.pump();

    expect(find.text('Şifreler eşleşmiyor.'), findsOneWidget);
  });

  testWidgets('Gezi formu boş kaydedilince ad ve tarih uyarıları görünür',
      (WidgetTester tester) async {
    await tester.pumpWidget(const MaterialApp(home: TripFormScreen()));

    await tester.tap(find.text('Geziyi Oluştur'));
    await tester.pump();

    expect(find.text('Gezi adı zorunludur.'), findsOneWidget);
    expect(find.text('Başlangıç tarihi zorunludur.'), findsOneWidget);
  });

  test('Şifre kuralı API ile aynı', () {
    expect(Validators.newPassword('123'), 'Şifre en az 6 karakter olmalıdır.');
    expect(Validators.newPassword('simge123'), 'Şifre en az bir büyük harf içermelidir.');
    expect(Validators.newPassword('Simge123'), isNull);
  });

  test('Gezi tarihleri ve davet kodu doğrulanır', () {
    final start = DateTime(2026, 11, 20);
    expect(Validators.tripDates(start, DateTime(2026, 11, 19)),
        'Bitiş tarihi başlangıç tarihinden önce olamaz.');
    expect(Validators.tripDates(start, start), isNull);
    expect(Validators.inviteCode('ABC'), 'Davet kodu 6 karakterdir.');
    expect(Validators.inviteCode('ABC234'), isNull);
  });

  test('Gezi içi role göre yetkiler', () {
    Trip withRole(String? role) => Trip.fromJson({
          'id': 1,
          'name': 'Test',
          'startDate': '2026-11-20',
          'endDate': '2026-11-23',
          'inviteCode': 'ABC234',
          'ownerName': 'Demo',
          'memberCount': 2,
          'myRole': role,
        });

    expect(withRole('Owner').isOwner, isTrue);
    expect(withRole('Editor').canEdit, isTrue);
    expect(withRole('Editor').isOwner, isFalse);
    expect(withRole('Viewer').canEdit, isFalse);
  });

  test('Mekan JSON ve kategori bilgisi doğru okunur', () {
    final place = Place.fromJson({
      'id': 7,
      'tripId': 1,
      'name': 'Göreme Açık Hava Müzesi',
      'category': 'Museum',
      'latitude': 38.6431,
      'longitude': 34.8455,
      'addedById': 'u1',
      'addedByName': 'Demo Kullanıcı',
      'score': 2,
      'upVotes': 3,
      'downVotes': 1,
      'myVote': 1,
      'canEdit': true,
    });

    expect(place.categoryInfo.label, 'Müze');
    expect(place.score, 2);
    expect(place.myVote, 1);
    expect(PlaceCategoryInfo.of('Bilinmeyen').label, 'Diğer');
  });

  test('Mekan adı doğrulanır', () {
    expect(Validators.placeName(''), 'Mekan adı zorunludur.');
    expect(Validators.placeName('A'), 'Mekan adı 2-150 karakter arasında olmalıdır.');
    expect(Validators.placeName('Uçhisar Kalesi'), isNull);
  });
}
