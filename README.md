# ContestPlacesWithoutPoints
Подсчёт мест в конкурсе (соревнованиях) без введения очковой системы

Контакты автора см. https://consp11.github.io/data/about.html

Справка - см. https://github.com/fdsc/ContestPlacesWithoutPoints/blob/master/help.md

Программа предназначена для оценки мест спортсменов без очковой системы.
Например, если гонщики проехали несколько гонок (известны их места), но очковую систему не хочется вводить, то эта программа сможет распределить гонщиков по местам чемпионата без введения очковой системы.
Преимущества алгоритма - отсутствие субъективности в выборе очков за каждое место.

# Запуск программы

Скачать дистрибутив можно по ссылке https://github.com/fdsc/ContestPlacesWithoutPoints/releases
Его нужно распаковать в любую папку (но не в Programs Files: там может не хватить прав для работы)

Для запуска программы необходимо
## На Windows
Установленная .NET версии 4.5 или выше
https://dotnet.microsoft.com/download

## На Linux
Установленная Mono
https://www.mono-project.com/

# Формальные правила оценки.
Предположим, что мы вводим правила распределение мест в гоночном чемпионате.
Мы уже знаем, на каких местах пришли гонщики в каждой гонке.

## Попарно оценим каждого гонщика с каждым.
Гонщик А был сильнее в гонке гонщика Б, если занял более высокое место в этой гонке.
Пусть N1 - количество гонок, где A сильнее Б, а N2 - количество гонок где Б сильнее А.
Гонщик А сильнее в чемпионате, чем гонщик Б, если N1 > N2.
Если N1 равно N2, то сильнее тот гонщик, у кого идёт превосходство в наиболее приоритетной гонке.
У гонщиков этого нет, но если наиболее приоритетный параметр (гонка) может быть равен у конкурсантов А и Б, то сравнивается следующий по приоритету параметр.

Если превосходства нет (все параметры абсолютно равны), то гонщики равны.

## Выберем гонщика, который лучше других
Если гонщик А сильнее всех других гонщиков, то он занимает первое место в итоговом зачёте.
Далее процедура подсчёта происходит полностью аналогично с самого начала, но уже без гонщика А (см. Продолжение процедуры).

### Если невозможно выбрать сильнейшего гонщика
Для всех гонщиков расчитаем параметр N, который говорит о том, над сколькими гонщиками сильнее этот гонщик.
Выберем Nmax - это максимальный параметр N из всех гонщиков.
Если гонщик, у которого N = Nmax только один, то он занимает первое место. Далее процедура расчёта происходит аналогично с самого начала, но уже без этого гонщика (см. Продолжение процедуры).

Если гонщиков, у которых N = Nmax несколько, то составляется список этих гонщиков. Этот список проходит процедуру выявления сильнейшего.
Сильнейший в этом списке занимает первое место. Далее процедура расчёта происходит аналогично с самого начала, но уже без этого гонщика (см. Продолжение процедуры).

Если найти сильнейшего невозможно, программа выдаёт ошибку.

## Продолжение процедуры
После того, как мы нашли сильнейшего списке конкурсантов, мы выдаём ему место и исключаем из списка.
Далее повторяем процедуру для оставшихся конкурсантов: находим нового сильнейшего (который получает следующее место), исключаем уже его и так далее.
