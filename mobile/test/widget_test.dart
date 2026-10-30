import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:ortakrota_mobile/screens/login_screen.dart';
import 'package:ortakrota_mobile/screens/register_screen.dart';
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

  test('Şifre kuralı API ile aynı', () {
    expect(Validators.newPassword('123'), 'Şifre en az 6 karakter olmalıdır.');
    expect(Validators.newPassword('simge123'), 'Şifre en az bir büyük harf içermelidir.');
    expect(Validators.newPassword('Simge123'), isNull);
  });
}
