# Sitonakator3000
Реализация некоего FTP-деплоера файлов с одной директории в другую (даже сетевую папку)

Писал для коммерческого проекта, где были проблемы с развертыванием CI/CD. 
В коде нет какого-то rocket-science, посему делюсь. Все равно разрабатывал по принципу StackOverflowDriveDevelopment.

## Принцип работы
Суть: программа по нажатию кнопки создает архив файлов/папок директории-таргета, удаляет их и переносит с директории-сорса. 
Есть возмоджность остановить и запустить службу IIS через команды powershell, 
так что удаленное управление сервером тоже присутствует. 
