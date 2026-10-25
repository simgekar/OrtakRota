import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:ortakrota_mobile/main.dart';

void main() {
  testWidgets('Giriş formu boş gönderilince doğrulama mesajları görünür',
      (WidgetTester tester) async {
    await tester.pumpWidget(const OrtakRotaApp());
    await tester.pumpAndSettle();

    expect(find.text('Giriş Yap'), findsOneWidget);

    await tester.tap(find.byType(FilledButton));
    await tester.pump();

    expect(find.text('E-posta zorunludur.'), findsOneWidget);
    expect(find.text('Şifre zorunludur.'), findsOneWidget);
  });
}
