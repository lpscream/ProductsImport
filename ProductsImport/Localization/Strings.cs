namespace ProductsImport.Localization;

/// <summary>
/// Every user-facing string in the app, keyed by a stable id, with Russian/Ukrainian/Romanian text.
/// <see cref="Current"/> is set once at startup (see Program.Main) from the saved app settings; forms
/// read it through <see cref="T(string)"/> in their field initializers, so a language change only takes
/// full effect after restarting the app (see "Настройки").
/// </summary>
public static class Strings
{
    public static Language Current { get; set; } = Language.Russian;

    // Index 0 = Russian, 1 = Ukrainian, 2 = Romanian - matches the Language enum's declaration order.
    private static readonly Dictionary<string, string[]> Map = new()
    {
        // ----- Common -----
        ["Common_OK"] = new[] { "ОК", "ОК", "OK" },
        ["Common_Cancel"] = new[] { "Отмена", "Скасувати", "Anulare" },
        ["Common_Close"] = new[] { "Закрыть", "Закрити", "Închide" },
        ["Common_Yes"] = new[] { "Да", "Так", "Da" },
        ["Common_No"] = new[] { "Нет", "Ні", "Nu" },
        ["Common_Row"] = new[] { "Строка", "Рядок", "Rând" },
        ["Common_Name"] = new[] { "Наименование", "Найменування", "Denumire" },
        ["Common_Warning"] = new[] { "Проверка", "Перевірка", "Verificare" },
        ["Common_Confirmation"] = new[] { "Подтверждение", "Підтвердження", "Confirmare" },
        ["Common_Done"] = new[] { "Готово", "Готово", "Gata" },
        ["Common_Error"] = new[] { "Ошибка", "Помилка", "Eroare" },
        ["Common_NoName"] = new[] { "(без наименования)", "(без найменування)", "(fără denumire)" },
        ["Common_NotSelectedValue"] = new[] { "— не выбрано —", "— не вибрано —", "— neselectat —" },
        ["Common_ValueInDocument"] = new[] { "Значение в документе", "Значення в документі", "Valoare în document" },
        ["Common_NotAvailable"] = new[] { "(нет)", "(немає)", "(niciunul)" },

        // ----- MainForm -----
        ["Main_Title"] = new[] { "Импорт товаров в справочник", "Імпорт товарів у довідник", "Import produse în catalog" },
        ["Main_BtnConnection"] = new[] { "Подключение к базе данных...", "Підключення до бази даних...", "Conectare la baza de date..." },
        ["Main_LblConnectionNotSelected"] = new[] { "Подключение не выбрано", "Підключення не вибрано", "Conexiune neselectată" },
        ["Main_BtnSettings"] = new[] { "Настройки...", "Налаштування...", "Setări..." },
        ["Main_BtnOpenFile"] = new[] { "Открыть документ...", "Відкрити документ...", "Deschide documentul..." },
        ["Main_LblFileNotOpen"] = new[] { "Файл не открыт", "Файл не відкрито", "Fișierul nu este deschis" },
        ["Main_LblSheet"] = new[] { "Лист:", "Аркуш:", "Foaie:" },
        ["Main_LblHeaderRow"] = new[] { "Строка с заголовками:", "Рядок із заголовками:", "Rândul cu anteturi:" },
        ["Main_LblImportProfile"] = new[] { "Профиль импорта:", "Профіль імпорту:", "Profil de import:" },
        ["Main_ImportProfileNone"] = new[] { "— не выбран —", "— не вибрано —", "— neselectat —" },
        ["Main_BtnSaveImportProfile"] = new[] { "Сохранить как профиль...", "Зберегти як профіль...", "Salvează ca profil..." },
        ["Main_BtnDeleteImportProfile"] = new[] { "Удалить профиль", "Видалити профіль", "Șterge profilul" },
        ["Main_OpenFileFilter"] = new[]
        {
            "Документы Excel/CSV (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|Все файлы (*.*)|*.*",
            "Документи Excel/CSV (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|Усі файли (*.*)|*.*",
            "Documente Excel/CSV (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|Toate fișierele (*.*)|*.*"
        },

        ["Main_Grid_ColIndex"] = new[] { "Колонка", "Колонка", "Coloană" },
        ["Main_Grid_ColHeader"] = new[] { "Заголовок в документе", "Заголовок у документі", "Antet în document" },
        ["Main_Grid_ColSample"] = new[] { "Пример значения", "Приклад значення", "Exemplu de valoare" },
        ["Main_Grid_ColTarget"] = new[] { "Назначение", "Призначення", "Destinație" },

        ["Field_None"] = new[] { "— не использовать —", "— не використовувати —", "— nu se utilizează —" },
        ["Field_Name"] = new[] { "Наименование (обязательно)", "Найменування (обов'язково)", "Denumire (obligatoriu)" },
        ["Field_Barcode"] = new[] { "Штрих-код", "Штрих-код", "Cod de bare" },
        ["Field_Group"] = new[] { "Группа", "Група", "Grupă" },
        ["Field_Article"] = new[] { "Артикул", "Артикул", "Cod articol" },
        ["Field_Weighted"] = new[] { "Весовой (да/нет)", "Ваговий (так/ні)", "Cântărit (da/nu)" },
        ["Field_Unit"] = new[] { "Единица измерения", "Одиниця виміру", "Unitate de măsură" },
        ["Field_TaxRate"] = new[] { "Налоговая ставка", "Податкова ставка", "Cotă de impozitare" },
        ["Field_Excise"] = new[] { "Подакцизный (да/нет)", "Підакцизний (так/ні)", "Accizabil (da/nu)" },
        ["Field_Uktzed"] = new[] { "УКТЗЕД", "УКТЗЕД", "UCTZED" },

        ["Barcode_ActionGenerate"] = new[] { "Сгенерировать штрих-код", "Згенерувати штрих-код", "Generează cod de bare" },
        ["Barcode_ActionSkip"] = new[] { "Не импортировать", "Не імпортувати", "Nu importa" },
        ["Barcode_ActionManual"] = new[] { "Ввести вручную", "Ввести вручну", "Introdu manual" },

        ["Main_MappingGroupTitle"] = new[] { "Сопоставление колонок", "Зіставлення колонок", "Corespondența coloanelor" },
        ["Main_BtnGroupMapping"] = new[] { "Сопоставление групп товаров", "Зіставлення груп товарів", "Corespondența grupurilor de produse" },
        ["Main_BtnUnitMapping"] = new[] { "Сопоставление единиц измерения", "Зіставлення одиниць виміру", "Corespondența unităților de măsură" },
        ["Main_BtnTaxRateMapping"] = new[] { "Сопоставление налоговых групп", "Зіставлення податкових груп", "Corespondența grupurilor fiscale" },
        ["Main_BtnExciseMapping"] = new[] { "Сопоставление акцизности товара", "Зіставлення акцизності товару", "Corespondența accizei produsului" },
        ["Main_MappingHint"] = new[]
        {
            "Наименование и штрих-код являются основными колонками. Остальные характеристики можно не сопоставлять — для них ниже можно указать значение по умолчанию.",
            "Найменування та штрих-код є основними колонками. Інші характеристики можна не зіставляти — для них нижче можна вказати значення за умовчанням.",
            "Denumirea și codul de bare sunt coloanele principale. Celelalte caracteristici pot rămâne nesetate — pentru ele mai jos poate fi indicată o valoare implicită."
        },

        ["Main_DefaultsGroupTitle"] = new[] { "Значения по умолчанию", "Значення за умовчанням", "Valori implicite" },
        ["Main_LblDefaultGroup"] = new[] { "Группа товаров по умолчанию:", "Група товарів за умовчанням:", "Grupa de produse implicită:" },
        ["Main_LblDefaultUnit"] = new[] { "Единица измерения по умолчанию:", "Одиниця виміру за умовчанням:", "Unitatea de măsură implicită:" },
        ["Main_LblDefaultVat"] = new[] { "Налоговая ставка по умолчанию:", "Податкова ставка за умовчанням:", "Cota de impozitare implicită:" },
        ["Main_LblWeighted"] = new[] { "Товар весовой:", "Товар ваговий:", "Produs cântărit:" },
        ["Main_LblExcise"] = new[] { "Товар подакцизный:", "Товар підакцизний:", "Produs accizabil:" },

        ["Main_BarcodeGroupTitle"] = new[] { "Требуется решение по штрих-коду", "Потрібне рішення щодо штрих-коду", "Este necesară o decizie privind codul de bare" },
        ["Main_BarcodeGroupTitleCount"] = new[]
        {
            "Требуется решение по штрих-коду ({0})",
            "Потрібне рішення щодо штрих-коду ({0})",
            "Este necesară o decizie privind codul de bare ({0})"
        },
        ["Main_LblForSelectedRows"] = new[] { "Для выделенных строк:", "Для виділених рядків:", "Pentru rândurile selectate:" },
        ["Main_BtnApplyToAll"] = new[] { "Применить ко всем", "Застосувати до всіх", "Aplică la toate" },
        ["Main_Grid_ColBarcodeRaw"] = new[] { "Штрих-код в документе", "Штрих-код у документі", "Codul de bare din document" },
        ["Main_Grid_ColAction"] = new[] { "Действие", "Дія", "Acțiune" },
        ["Main_Grid_ColManual"] = new[] { "Штрих-код вручную", "Штрих-код вручну", "Cod de bare manual" },

        ["Main_BtnStartImport"] = new[] { "Начать импорт...", "Почати імпорт...", "Începe importul..." },
        ["Main_BtnCommitImport"] = new[] { "Импортировать", "Імпортувати", "Importă" },

        ["Main_Msg_RefDataFailed"] = new[] { "Не удалось получить справочные данные из базы: {0}", "Не вдалося отримати довідкові дані з бази: {0}", "Nu s-au putut obține datele de referință din baza de date: {0}" },
        ["Main_Msg_OpenFileFailed"] = new[] { "Не удалось открыть файл: {0}", "Не вдалося відкрити файл: {0}", "Nu s-a putut deschide fișierul: {0}" },
        ["Main_Msg_OpenDocumentFirst"] = new[] { "Сначала откройте документ с товарами.", "Спочатку відкрийте документ із товарами.", "Mai întâi deschideți documentul cu produse." },
        ["Main_Msg_InvalidHeaderRow"] = new[] { "Некорректно указана строка с заголовками.", "Некоректно вказано рядок із заголовками.", "Rândul cu anteturi este indicat incorect." },
        ["Main_Msg_NoDataAfterHeader"] = new[] { "После указанной строки заголовков нет данных.", "Після вказаного рядка заголовків немає даних.", "După rândul de antet indicat nu există date." },
        ["Main_Msg_FieldMappedTwice"] = new[]
        {
            "Поле \"{0}\" сопоставлено сразу нескольким колонкам ({1} и {2}). Каждое поле можно сопоставить только один раз.",
            "Поле \"{0}\" зіставлено одразу з кількома колонками ({1} і {2}). Кожне поле можна зіставити лише один раз.",
            "Câmpul \"{0}\" este asociat mai multor coloane deodată ({1} și {2}). Fiecare câmp poate fi asociat o singură dată."
        },
        ["Main_Msg_NameNotMapped"] = new[] { "Необходимо сопоставить колонку с наименованием товара.", "Необхідно зіставити колонку з найменуванням товару.", "Este necesar să asociați o coloană cu denumirea produsului." },
        ["Main_Msg_BarcodeNotMappedConfirm"] = new[]
        {
            "Колонка со штрих-кодом не указана. Штрих-код нужно будет сгенерировать, ввести вручную или пропустить для каждого товара. Продолжить?",
            "Колонку зі штрих-кодом не вказано. Штрих-код потрібно буде згенерувати, ввести вручну або пропустити для кожного товару. Продовжити?",
            "Coloana cu codul de bare nu este indicată. Codul de bare va trebui generat, introdus manual sau omis pentru fiecare produs. Continuați?"
        },
        ["Main_Title_BarcodeNotMapped"] = new[] { "Штрих-код не сопоставлен", "Штрих-код не зіставлено", "Codul de bare nu este asociat" },
        ["Main_Msg_ConnectFirst"] = new[] { "Сначала подключитесь к базе данных.", "Спочатку підключіться до бази даних.", "Mai întâi conectați-vă la baza de date." },
        ["Main_Msg_ChooseConnectionFirst"] = new[] { "Сначала выберите подключение к базе данных.", "Спочатку виберіть підключення до бази даних.", "Mai întâi selectați o conexiune la baza de date." },
        ["Main_Msg_NoValuesToMap"] = new[] { "В сопоставленной колонке нет значений для сопоставления.", "У зіставленій колонці немає значень для зіставлення.", "În coloana asociată nu există valori de asociat." },
        ["Main_Msg_ProfileExistsConfirm"] = new[] { "Профиль \"{0}\" уже существует. Заменить его?", "Профіль \"{0}\" вже існує. Замінити його?", "Profilul \"{0}\" există deja. Îl înlocuiți?" },
        ["Main_Msg_ProfileSaved"] = new[] { "Профиль сохранён.", "Профіль збережено.", "Profilul a fost salvat." },
        ["Main_Msg_SelectProfileFromList"] = new[] { "Выберите профиль из списка.", "Виберіть профіль зі списку.", "Selectați un profil din listă." },
        ["Main_Msg_DeleteProfileConfirm"] = new[] { "Удалить профиль \"{0}\"?", "Видалити профіль \"{0}\"?", "Ștergeți profilul \"{0}\"?" },
        ["Main_Msg_NoDefaultGroupConfirm"] = new[]
        {
            "Не выбрана группа по умолчанию. Товары без определённой группы не будут импортированы. Продолжить?",
            "Не вибрано групу за умовчанням. Товари без визначеної групи не будуть імпортовані. Продовжити?",
            "Nu este selectată grupa implicită. Produsele fără o grupă determinată nu vor fi importate. Continuați?"
        },
        ["Main_Msg_NoDefaultUnitConfirm"] = new[]
        {
            "Не выбрана единица измерения по умолчанию. Товары без определённой единицы измерения не будут импортированы. Продолжить?",
            "Не вибрано одиницю виміру за умовчанням. Товари без визначеної одиниці виміру не будуть імпортовані. Продовжити?",
            "Nu este selectată unitatea de măsură implicită. Produsele fără o unitate de măsură determinată nu vor fi importate. Continuați?"
        },
        ["Main_Msg_NoDefaultVatConfirm"] = new[]
        {
            "Не выбрана налоговая ставка по умолчанию. Товары без определённой ставки не будут импортированы. Продолжить?",
            "Не вибрано податкову ставку за умовчанням. Товари без визначеної ставки не будуть імпортовані. Продовжити?",
            "Nu este selectată cota de impozitare implicită. Produsele fără o cotă determinată nu vor fi importate. Continuați?"
        },
        ["Main_Msg_BarcodeNotEnteredRow"] = new[]
        {
            "Строка {0}: не введён штрих-код. Нажмите \"...\", чтобы ввести его.",
            "Рядок {0}: не введено штрих-код. Натисніть \"...\", щоб ввести його.",
            "Rândul {0}: codul de bare nu a fost introdus. Apăsați \"...\" pentru a-l introduce."
        },
        ["Main_Msg_BarcodeAlreadyUsedRow"] = new[]
        {
            "Строка {0}: указанный штрих-код уже используется другим товаром. Введите другой.",
            "Рядок {0}: вказаний штрих-код уже використовується іншим товаром. Введіть інший.",
            "Rândul {0}: codul de bare indicat este deja folosit de alt produs. Introduceți altul."
        },
        ["Main_Msg_NoDataRows"] = new[] { "Не найдено ни одной строки с данными.", "Не знайдено жодного рядка з даними.", "Nu a fost găsit niciun rând cu date." },
        ["Main_Msg_ImportError"] = new[] { "Ошибка импорта: {0}", "Помилка імпорту: {0}", "Eroare de import: {0}" },

        ["Main_SaveProfileTitle"] = new[] { "Сохранить профиль", "Зберегти профіль", "Salvează profilul" },
        ["Main_SaveProfilePrompt"] = new[] { "Название профиля импорта:", "Назва профілю імпорту:", "Denumirea profilului de import:" },

        // ----- ValueMappingForm -----
        ["ValueMap_ColVatHeader"] = new[] { "Ставка НДС", "Ставка ПДВ", "Cota TVA" },
        ["ValueMap_ColExciseHeader"] = new[] { "Подакцизный", "Підакцизний", "Accizabil" },

        // ----- ConnectionSettingsForm -----
        ["ConnList_Title"] = new[] { "Подключения к базе данных", "Підключення до бази даних", "Conexiuni la baza de date" },
        ["ConnList_BtnAdd"] = new[] { "Добавить...", "Додати...", "Adaugă..." },
        ["ConnList_BtnEdit"] = new[] { "Изменить...", "Змінити...", "Editează..." },
        ["ConnList_BtnDelete"] = new[] { "Удалить", "Видалити", "Șterge" },
        ["ConnList_BtnSelect"] = new[] { "Выбрать", "Вибрати", "Selectează" },
        ["ConnList_Msg_SelectFromList"] = new[] { "Выберите подключение из списка.", "Виберіть підключення зі списку.", "Selectați o conexiune din listă." },
        ["ConnList_Msg_DeleteConfirm"] = new[] { "Удалить подключение \"{0}\"?", "Видалити підключення \"{0}\"?", "Ștergeți conexiunea \"{0}\"?" },

        // ----- ConnectionEditForm -----
        ["ConnEdit_TitleNew"] = new[] { "Новое подключение", "Нове підключення", "Conexiune nouă" },
        ["ConnEdit_TitleEdit"] = new[] { "Изменить подключение", "Змінити підключення", "Editează conexiunea" },
        ["ConnEdit_LblName"] = new[] { "Название:", "Назва:", "Denumire:" },
        ["ConnEdit_LblServer"] = new[] { "Сервер:", "Сервер:", "Server:" },
        ["ConnEdit_LblDatabase"] = new[] { "База данных:", "База даних:", "Bază de date:" },
        ["ConnEdit_LblLogin"] = new[] { "Логин:", "Логін:", "Utilizator:" },
        ["ConnEdit_LblPassword"] = new[] { "Пароль:", "Пароль:", "Parolă:" },
        ["ConnEdit_ChkWindowsAuth"] = new[] { "Использовать Windows-аутентификацию", "Використовувати автентифікацію Windows", "Utilizează autentificarea Windows" },
        ["ConnEdit_BtnTest"] = new[] { "Проверить", "Перевірити", "Testează" },
        ["ConnEdit_BtnSave"] = new[] { "Сохранить", "Зберегти", "Salvează" },
        ["ConnEdit_Msg_FillRequired"] = new[] { "Заполните название, сервер и базу данных.", "Заповніть назву, сервер і базу даних.", "Completați denumirea, serverul și baza de date." },
        ["ConnEdit_Msg_SpecifyServerAndDb"] = new[] { "Укажите сервер и базу данных", "Вкажіть сервер і базу даних", "Indicați serverul și baza de date" },
        ["ConnEdit_Msg_ConnectionOk"] = new[] { "Подключение успешно", "Підключення успішне", "Conexiune reușită" },
        ["ConnEdit_Msg_ConnectionError"] = new[] { "Ошибка: {0}", "Помилка: {0}", "Eroare: {0}" },

        // ----- ImportResultForm -----
        ["Result_Title"] = new[] { "Результат импорта", "Результат імпорту", "Rezultatul importului" },
        ["Result_Summary"] = new[]
        {
            "Всего строк: {0}\r\nУспешно импортировано: {1}\r\nПропущено пользователем: {2}\r\nНе импортировано из-за ошибок: {3}",
            "Усього рядків: {0}\r\nУспішно імпортовано: {1}\r\nПропущено користувачем: {2}\r\nНе імпортовано через помилки: {3}",
            "Total rânduri: {0}\r\nImportate cu succes: {1}\r\nOmise de utilizator: {2}\r\nNeimportate din cauza erorilor: {3}"
        },
        ["Result_BtnExport"] = new[] { "Сохранить ошибки в Excel...", "Зберегти помилки в Excel...", "Salvează erorile în Excel..." },
        ["Result_SaveDialogFilter"] = new[] { "Книга Excel (*.xlsx)|*.xlsx", "Книга Excel (*.xlsx)|*.xlsx", "Registru Excel (*.xlsx)|*.xlsx" },
        ["Result_Msg_Saved"] = new[] { "Файл с ошибками сохранён.", "Файл із помилками збережено.", "Fișierul cu erori a fost salvat." },
        ["Result_Msg_SaveFailed"] = new[] { "Не удалось сохранить файл: {0}", "Не вдалося зберегти файл: {0}", "Nu s-a putut salva fișierul: {0}" },

        // ----- ManualBarcodeForm -----
        ["Manual_Title"] = new[] { "Ввод штрих-кода вручную", "Введення штрих-коду вручну", "Introducere manuală a codului de bare" },
        ["Manual_LblProduct"] = new[] { "Товар: {0}", "Товар: {0}", "Produs: {0}" },
        ["Manual_Msg_InvalidBarcode"] = new[] { "Штрих-код должен содержать 8, 12 или 13 цифр.", "Штрих-код повинен містити 8, 12 або 13 цифр.", "Codul de bare trebuie să conțină 8, 12 sau 13 cifre." },

        // ----- TextPromptForm -----
        ["TextPrompt_Msg_EnterValue"] = new[] { "Введите значение.", "Введіть значення.", "Introduceți o valoare." },

        // ----- ProductGroupMappingForm -----
        ["GroupMap_TitleFormat"] = new[] { "Сопоставление групп товаров ({0})", "Зіставлення груп товарів ({0})", "Corespondența grupurilor de produse ({0})" },
        ["GroupMap_BtnApply"] = new[] { "Применить к выделенным", "Застосувати до виділених", "Aplică la cele selectate" },
        ["GroupMap_Msg_SelectGroup"] = new[] { "Выберите группу из списка.", "Виберіть групу зі списку.", "Selectați o grupă din listă." },
        ["GroupMap_Msg_SelectRows"] = new[] { "Выделите хотя бы одну строку товара.", "Виділіть хоча б один рядок товару.", "Selectați cel puțin un rând de produs." },
        ["GroupMap_ColCurrent"] = new[] { "Текущая группа", "Поточна група", "Grupa curentă" },
        ["GroupMap_ColNew"] = new[] { "Новая группа", "Нова група", "Grupă nouă" },
        ["GroupMap_PendingCreate"] = new[] { "(будет создана: {0})", "(буде створено: {0})", "(va fi creată: {0})" },
        ["GroupMap_NoOverride"] = new[] { "— не менять —", "— не змінювати —", "— nu se modifică —" },

        // ----- ImportOrchestrator row errors -----
        ["Err_NoName"] = new[] { "Не указано наименование товара", "Не вказано найменування товару", "Denumirea produsului nu este specificată" },
        ["Err_SkippedNoBarcode"] = new[] { "Пропущено пользователем: нет штрих-кода", "Пропущено користувачем: немає штрих-коду", "Omis de utilizator: lipsește codul de bare" },
        ["Err_NoGroup"] = new[] { "Не удалось определить группу товара", "Не вдалося визначити групу товару", "Nu s-a putut determina grupa produsului" },
        ["Err_NoUnit"] = new[] { "Не удалось определить единицу измерения", "Не вдалося визначити одиницю виміру", "Nu s-a putut determina unitatea de măsură" },
        ["Err_NoVat"] = new[] { "Не удалось определить ставку НДС", "Не вдалося визначити ставку ПДВ", "Nu s-a putut determina cota TVA" },
        ["Err_ArticleTaken"] = new[] { "Артикул {0} уже используется другим товаром", "Артикул {0} уже використовується іншим товаром", "Codul de articol {0} este deja folosit de alt produs" },
        ["Err_BarcodeUnresolved"] = new[] { "Штрих-код не был разрешён перед импортом", "Штрих-код не було визначено перед імпортом", "Codul de bare nu a fost rezolvat înainte de import" },
        ["Err_DbError"] = new[] { "Ошибка базы данных: {0}", "Помилка бази даних: {0}", "Eroare de bază de date: {0}" },

        // ----- ErrorReportService -----
        ["ErrRep_SheetName"] = new[] { "Ошибки импорта", "Помилки імпорту", "Erori de import" },
        ["ErrRep_ColumnFallback"] = new[] { "Колонка {0}", "Колонка {0}", "Coloana {0}" },
        ["ErrRep_ColHeader"] = new[] { "Ошибка импорта", "Помилка імпорту", "Eroare de import" },

        // ----- Settings -----
        ["Settings_Title"] = new[] { "Настройки", "Налаштування", "Setări" },
        ["Settings_LblLanguage"] = new[] { "Язык интерфейса:", "Мова інтерфейсу:", "Limba interfeței:" },
        ["Settings_LangRussian"] = new[] { "Русский", "Російська", "Rusă" },
        ["Settings_LangUkrainian"] = new[] { "Украинский", "Українська", "Ucraineană" },
        ["Settings_LangRomanian"] = new[] { "Румынский", "Румунська", "Română" },
        ["Settings_RestartNotice"] = new[]
        {
            "Изменения вступят в силу после перезапуска программы.",
            "Зміни набудуть чинності після перезапуску програми.",
            "Modificările vor intra în vigoare după repornirea programului."
        }
    };

    public static string T(string key)
    {
        if (Map.TryGetValue(key, out var byLanguage))
        {
            var index = (int)Current;
            if (index >= 0 && index < byLanguage.Length && !string.IsNullOrEmpty(byLanguage[index]))
            {
                return byLanguage[index];
            }
        }

        return key;
    }

    public static string T(string key, params object[] args) => string.Format(T(key), args);
}
