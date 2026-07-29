Proyecto final: Artemis Banking Pro (ABP)
[2026] Documento Funcional
Versión 1.0
30 de junio, 2026
© Itla 2026
Autopista Las Américas, Km. 27, PCSD,
La Caleta, Boca Chica 11606.
Tel. 809-738-4852

Índice
Objetivo general 3
Funcionalidades generales 3
Login 3
Seguridad 12
Funcionalidades Administrador 16
Dashboard(Home) 16
Gestión de usuarios 20
Gestión de préstamos 32
Gestión de tarjetas de crédito 47
Gestión de cuentas de ahorro 59
Funcionalidades cliente 69
Funcionalidad de Beneficiarios 77
Funcionalidad de transacciones 81
Funcionalidad de Avances de efectivo 94
Funcionalidad de Transferencia entre cuentas 100
Funcionalidades Cajero 105
Home 105
Depósito 108
Retiro 112
Pago a tarjeta de crédito 117
Pago a préstamo 123
Transacciones a cuentas de terceros 130
Funcionalidades del API 136
Seguridad 136
Módulo: Login y Account Controller 141
Módulo: Gestión de Usuarios 148
Módulo: Gestión de Préstamos 165
Módulo: Gestión de Tarjetas de Crédito 176
Módulo: Gestión de Cuentas de Ahorro 186
Módulo: Gestión de Comercios 195
Módulo: Procesador de Pago (Hermes Pay) 206
Requerimientos técnicos 217

Objetivo general
Desarrollar una plataforma de banca digital que permita gestionar de manera
integral las principales operaciones de una entidad financiera, incluyendo la
administración de usuarios, cuentas de ahorro, préstamos, tarjetas de crédito, pagos,
depósitos, retiros, transferencias, beneficiarios y avances de efectivo.
El sistema debe permitir que los administradores gestionen los productos
financieros de los clientes, asignen cuentas de ahorro, préstamos y tarjetas de
crédito, consulte indicadores generales del sistema y administren los usuarios que
tendrán acceso a la plataforma.
De igual forma, la aplicación debe permitir que los cajeros realicen operaciones
operativas como depósitos, retiros, pagos a tarjetas de crédito, pagos a préstamos y
transacciones a cuentas de terceros, manteniendo el registro correspondiente de
cada movimiento realizado.
Los clientes deben poder consultar sus productos financieros activos, visualizar sus
movimientos, gestionar beneficiarios, realizar pagos, transferencias entre cuentas,
transacciones a terceros y avances de efectivo desde sus tarjetas de crédito hacia
sus cuentas de ahorro.
Además, el sistema debe incluir una Web API que permita exponer funcionalidades
administrativas y de comercio, incluyendo la gestión de comercios y el
procesamiento de pagos mediante el módulo Hermes Pay.
La aplicación debe desarrollarse utilizando ASP.NET Core MVC con .NET 9 para la
parte web y una Web API para los servicios externos, manteniendo una estructura
segura, organizada y basada en roles.
El sistema debe garantizar que cada usuario acceda únicamente a las
funcionalidades correspondientes a su rol, preservar la integridad de los balances,
deudas y transacciones, y mantener trazabilidad de todas las operaciones
financieras realizadas dentro de la plataforma.
Funcionalidades generales
Login
La pantalla de Login será la pantalla inicial de la aplicación web y permitirá que los
usuarios con rol Administrador, Cajero o Cliente puedan acceder al sistema
mediante sus credenciales.

Si un usuario ya se encuentra autenticado e intenta acceder nuevamente a la
pantalla de Login, el sistema no debe mostrar el formulario. En ese caso, debe
redirigir automáticamente al usuario al Home correspondiente a su rol.
La redirección debe funcionar de la siguiente manera:
| Rol del usuario autenticado  |                         | Pantalla de destino  |     |     |
| ---------------------------- | ----------------------- | -------------------- | --- | --- |
| Administrador                | Home del administrador  |                      |     |     |
| Cajero                       | Home del cajero         |                      |     |     |
| Cliente                      | Home del cliente        |                      |     |     |
Los usuarios con rol Comercio no deben iniciar sesión desde la aplicación web. Este
rol será utilizado únicamente para las funcionalidades de la Web API y el
procesador de pagos Hermes Pay.
Pantalla de inicio de sesión
La pantalla de Login debe mostrar un formulario para que el usuario pueda ingresar
sus credenciales.
El formulario debe contener los siguientes campos:
| Campo  | Tipo de dato  | Requerido  |     | Descripción  |
| ------ | ------------- | ---------- | --- | ------------ |
Nombre de usuario  Texto / string  Sí  Nombre de usuario registrado en el sistem
| Contraseña  | Password /  | Sí  | Contraseña asociada al usuario.  |     |
| ----------- | ----------- | --- | -------------------------------- | --- |
string
Debajo del formulario debe existir un botón con el texto Iniciar sesión.
Además, la pantalla debe incluir una opción llamada Restablecer contraseña, la cual
enviará al usuario al proceso de recuperación de contraseña.
Validaciones de inicio de sesión
El formulario de inicio de sesión debe cumplir las siguientes validaciones:
● El nombre de usuario es requerido.
 ● La contraseña es requerida.
 ● Las credenciales deben coincidir con un usuario registrado.
 ● El usuario debe estar activo.

● El usuario debe tener uno de los roles permitidos para la aplicación web:
Administrador, Cajero o Cliente.
Si el nombre de usuario o la contraseña son incorrectos, el sistema debe mostrar el
siguiente mensaje:
“Los datos de acceso son inválidos.”
Si el usuario existe, pero su cuenta se encuentra inactiva, el sistema debe rechazar
el inicio de sesión y mostrar el siguiente mensaje:
“Su cuenta se encuentra inactiva. Debe activar su cuenta mediante el enlace
enviado a su correo electrónico registrado para poder acceder al sistema.”
Si el usuario tiene un rol que no corresponde a la aplicación web, el sistema debe
mostrar el siguiente mensaje:
“Este usuario no tiene permisos para acceder a la aplicación web.”
Si el usuario tiene rol Administrador y sus credenciales son válidas, el sistema debe
redirigirlo al Home del administrador.
Si el usuario tiene rol Cajero y sus credenciales son válidas, el sistema debe
redirigirlo al Home del cajero.
Si el usuario tiene rol Cliente y sus credenciales son válidas, el sistema debe
redirigirlo al Home del cliente.
Activación de cuenta
Los usuarios creados desde el sistema deben quedar inicialmente inactivos.
Luego de crear un usuario, el sistema debe enviar un correo electrónico al correo
registrado con un enlace de activación.
Cuando el usuario haga clic sobre el enlace de activación, el sistema debe validar el
token recibido.
El token de activación debe cumplir las siguientes condiciones:
Elemento Descripción
Relación Debe estar asociado al usuario creado.
Uso Debe poder utilizarse una sola vez.

Estado Debe permitir activar la cuenta del usuario.
Si el token de activación es válido, el sistema debe activar la cuenta del usuario y
redirigirlo a la pantalla de Login.
Después de activar correctamente la cuenta, el sistema debe mostrar el siguiente
mensaje:
“Su cuenta ha sido activada correctamente. Ya puede iniciar sesión.”
Si el token no es válido, el sistema debe mostrar el siguiente mensaje:
“El enlace de activación no es válido.”
Si el token ya fue utilizado anteriormente, el sistema debe mostrar el siguiente
mensaje:
“Este enlace de activación ya fue utilizado.”
Restablecimiento de contraseña
En la pantalla de Login debe existir una opción llamada Restablecer contraseña.
Al hacer clic sobre esta opción, el sistema debe enviar al usuario a una nueva
pantalla o mostrar un modal con un formulario para solicitar el restablecimiento de
contraseña.
El formulario debe contener los siguientes campos:
Campo Tipo de dato Requerido Descripción
Nombre de usuario Texto / string Sí Nombre de usuario de la
cuenta para la cual se desea
restablecer la contraseña.
Debajo del formulario debe existir un botón con el texto Enviar solicitud.
Validaciones al solicitar restablecimiento de contraseña
El formulario de solicitud de restablecimiento debe cumplir las siguientes
validaciones:
● El nombre de usuario es requerido.
● El nombre de usuario debe existir en el sistema.

● El usuario debe tener un correo electrónico registrado.
● El usuario debe tener uno de los roles permitidos para la aplicación web:
Administrador, Cajero o Cliente.
Si el nombre de usuario no existe, el sistema debe mostrar el siguiente mensaje:
“No existe un usuario registrado con este nombre de usuario.”
Si el usuario no tiene un correo electrónico registrado, el sistema debe mostrar el
siguiente mensaje:
“Este usuario no tiene un correo electrónico registrado. No es posible enviar la
solicitud de restablecimiento.”
Si el usuario existe y tiene correo electrónico registrado, el sistema debe realizar las
siguientes acciones:
1. Desactivar temporalmente la cuenta del usuario.
2. Generar un token único de restablecimiento de contraseña.
3. Asociar el token generado al usuario.
4. Guardar la fecha y hora de generación del token.
5. Enviar un correo electrónico al usuario con un enlace de restablecimiento.
6. Mostrar un mensaje indicando que la solicitud fue enviada correctamente.
El token de restablecimiento debe cumplir las siguientes condiciones:
Elemento Descripción
Relación Debe estar asociado al usuario que solicitó el restablecimiento.
Vigencia Debe tener una duración máxima de 30 minutos.
Uso Debe poder utilizarse una sola vez.
Debe quedar inválido luego de cambiar la contraseña
Estado
correctamente.
Después de generar correctamente la solicitud, el sistema debe mostrar el siguiente
mensaje:
“Se ha enviado un enlace de restablecimiento de contraseña al correo electrónico
registrado.”

Correo de restablecimiento de contraseña
El sistema debe enviar un correo electrónico al usuario con el enlace para completar
el proceso de restablecimiento.
El correo puede tener un contenido como el siguiente:
Asunto: Restablecimiento de contraseña
Hola [Nombre del usuario],
Hemos recibido una solicitud para restablecer la contraseña de su cuenta.
Para continuar, haga clic en el siguiente enlace:
[ENLACE_DE_RESTABLECIMIENTO]
Este enlace tendrá una vigencia de 30 minutos.
Si usted no solicitó este cambio, ignore este mensaje.
Pantalla para crear nueva contraseña
Cuando el usuario haga clic sobre el enlace enviado por correo, el sistema debe
validar el token recibido como parámetro.
Si el token es válido, el sistema debe mostrar una pantalla con un formulario para
crear una nueva contraseña.
El formulario debe contener los siguientes campos:
Campo Tipo de dato Requerido Descripción
Contraseña Password / Sí Nueva contraseña que será asignada al
string usuario.
Confirmar Password / Sí Confirmación de la nueva contraseña
contraseña string ingresada.
Debajo del formulario debe existir un botón con el texto Restablecer contraseña.
Validaciones al crear nueva contraseña
El formulario para crear la nueva contraseña debe cumplir las siguientes

validaciones:
● El token debe existir.
● El token debe pertenecer al usuario correspondiente.
● El token no debe estar expirado.
● El token no debe haber sido utilizado anteriormente.
● La contraseña es requerida.
● La confirmación de contraseña es requerida.
● La contraseña y la confirmación de contraseña deben coincidir.
Si el token no existe o no pertenece a un usuario válido, el sistema debe mostrar el
siguiente mensaje:
“El enlace de restablecimiento no es válido.”
Si el token está expirado, el sistema debe mostrar el siguiente mensaje:
“El enlace de restablecimiento ha expirado. Solicite un nuevo restablecimiento de
contraseña.”
Si el token ya fue utilizado, el sistema debe mostrar el siguiente mensaje:
“Este enlace de restablecimiento ya fue utilizado.”
Si las contraseñas no coinciden, el sistema debe mostrar el siguiente mensaje:
“La contraseña y la confirmación de contraseña deben coincidir.”
Si todas las validaciones son correctas, el sistema debe actualizar la contraseña del
usuario, marcar el token como utilizado, activar nuevamente la cuenta y redirigir al
usuario a la pantalla de Login.
Después de completar el proceso, el sistema debe mostrar el siguiente mensaje:
“Su contraseña ha sido restablecida correctamente. Ya puede iniciar sesión.”
Seguridad
El sistema debe implementar autenticación y autorización por roles para proteger
todas las funcionalidades internas de la aplicación web.
Si un usuario no autenticado intenta acceder directamente a una ruta protegida, el
sistema debe redirigirlo a la pantalla de Login y mostrar el siguiente mensaje:
“No tiene permiso para acceder a esta sección.”

La aplicación web debe manejar los siguientes roles:
Rol Descripción
Usuario encargado de administrar usuarios, préstamos,
Administrador tarjetas de crédito, cuentas de ahorro e indicadores
generales del sistema.
Usuario encargado de realizar depósitos, retiros, pagos a
Cajero tarjetas de crédito, pagos a préstamos y transacciones a
cuentas de terceros.
Usuario que puede consultar sus productos financieros,
gestionar beneficiarios, realizar transacciones, pagos,
Cliente
avances de efectivo y transferencias entre sus propias
cuentas.
El rol Comercio no debe tener acceso a las funcionalidades de la aplicación web.
Este rol será utilizado únicamente para la Web API y el procesador de pagos
Hermes Pay.
Reglas de acceso por rol
Cada usuario debe acceder únicamente a las funcionalidades correspondientes a su
rol.
● Un usuario con rol Cliente no puede acceder a las funcionalidades del
Administrador ni del Cajero.
● Un usuario con rol Administrador no puede acceder a las funcionalidades del
Cliente ni del Cajero.
● Un usuario con rol Cajero no puede acceder a las funcionalidades del
Administrador ni del Cliente.
● Un usuario con rol Comercio no puede acceder a ninguna funcionalidad de la
aplicación web.
Si un usuario autenticado intenta acceder directamente por URL a una sección que
no corresponde a su rol, el sistema debe redirigirlo a una pantalla de Acceso
denegado.
Pantalla de Acceso denegado
La pantalla de Acceso denegado debe mostrarse cuando un usuario autenticado
intenta acceder a una funcionalidad para la cual no tiene permisos.

Esta pantalla debe mostrar el siguiente mensaje:
“No posee permisos para acceder a esta sección.”
Además, debe incluir un enlace o botón para regresar al Home correspondiente al
rol del usuario autenticado.
El enlace debe comportarse de la siguiente manera:
● Si el usuario tiene rol Administrador, debe enviarlo al Home del administrador.
● Si el usuario tiene rol Cajero, debe enviarlo al Home del cajero.
● Si el usuario tiene rol Cliente, debe enviarlo al Home del cliente.
● Si el usuario no está autenticado, debe enviarlo a la pantalla de Login.
Implementación de seguridad
Todas las restricciones de acceso deben implementarse utilizando ASP.NET Identity
y los filtros de autorización de ASP.NET Core.
Los controladores y acciones protegidas deben utilizar filtros de autorización por rol.
El sistema no debe depender únicamente de ocultar opciones en el menú. Aunque
una opción no se muestre visualmente, también debe impedirse el acceso directo
por URL a las rutas restringidas.
El menú de navegación debe mostrar únicamente las opciones permitidas para el
rol del usuario autenticado.
La opción Cerrar sesión debe estar disponible para todos los usuarios autenticados
de la aplicación web.
Al hacer clic en Cerrar sesión, el sistema debe cerrar la sesión activa y redirigir al
usuario a la pantalla de Login.
Usuarios creados por defecto
El sistema debe crear mediante seeding los roles principales de la aplicación web.
Los roles creados por defecto deben ser:
● Administrador.
● Cajero.
● Cliente.
Además, el sistema debe crear un usuario por defecto para cada rol:

● Un usuario administrador por defecto.
● Un usuario cajero por defecto.
● Un usuario cliente por defecto.
Estos usuarios por defecto deben crearse activos, ya que serán utilizados para
acceder inicialmente al sistema y probar las funcionalidades principales.
Los usuarios creados posteriormente desde la Gestión de usuarios deben quedar
inicialmente inactivos y deben completar el proceso de activación por correo
electrónico.
Reglas adicionales del módulo
● La pantalla de Login solo debe estar disponible para usuarios no autenticados.
● Si un usuario autenticado intenta acceder al Login, debe ser redirigido al Home
correspondiente a su rol.
● Solo los usuarios activos pueden iniciar sesión.
● Los usuarios inactivos no pueden acceder al sistema hasta completar su
activación o restablecer correctamente su contraseña.
● Todas las funcionalidades internas requieren autenticación.
● Cada usuario autenticado solo puede acceder a las funcionalidades
correspondientes a su rol.
● El acceso directo por URL debe estar protegido mediante autorización por rol.
● La pantalla de Acceso denegado debe mostrarse cuando un usuario autenticado
intenta acceder a una sección no permitida.
● Los usuarios con rol Comercio no deben acceder a la aplicación web.
● Los usuarios por defecto creados mediante seeding deben estar activos.
● Los usuarios creados manualmente desde el sistema deben quedar inactivos
hasta completar el proceso de activación por correo.
Seguridad
El sistema debe implementar autenticación y autorización por roles para proteger
todas las funcionalidades internas de la aplicación web.
Ningún usuario debe poder acceder a funcionalidades protegidas si no ha iniciado
sesión. Si un usuario no autenticado intenta acceder directamente a una ruta interna
del sistema, el sistema debe redirigirlo a la pantalla de Login.
En ese caso, debe mostrarse el siguiente mensaje:
“No tiene permiso para acceder a esta sección.”
La seguridad debe aplicarse tanto en el menú de navegación como en los

controladores y acciones del sistema. No basta con ocultar opciones visualmente; el
sistema también debe impedir el acceso directo mediante URL.
Roles de la aplicación web
La aplicación web debe manejar los siguientes roles:
Rol Descripción
Usuario encargado de administrar usuarios,
Administrador préstamos, tarjetas de crédito, cuentas de ahorro e
indicadores generales del sistema.
Usuario encargado de realizar depósitos, retiros,
Cajero pagos a tarjetas de crédito, pagos a préstamos y
transacciones a cuentas de terceros.
Usuario que puede consultar sus productos
financieros, gestionar beneficiarios, realizar
Cliente
transacciones, pagos, avances de efectivo y
transferencias entre sus propias cuentas.
El rol Comercio no debe tener acceso a la aplicación web. Este rol será utilizado
únicamente para las funcionalidades de la Web API y el procesador de pagos
Hermes Pay.
Reglas de acceso por rol
Cada usuario debe acceder únicamente a las funcionalidades correspondientes a su
rol.
● Un usuario con rol cliente no puede acceder a las funcionalidades del
Administrador ni del Cajero.
● Un usuario con rol Administrador no puede acceder a las funcionalidades del
Cliente ni del Cajero.
● Un usuario con rol Cajero no puede acceder a las funcionalidades del
Administrador ni del Cliente.
● Un usuario con rol Comercio no puede acceder a ninguna funcionalidad de la
aplicación web.
Si un usuario autenticado intenta acceder directamente por URL a una sección para

la cual no tiene permisos, el sistema debe impedir el acceso y redirigirlo a una
pantalla de acceso denegado.
Pantalla de Acceso denegado
La pantalla de Acceso denegado debe mostrarse cuando un usuario autenticado
intenta acceder a una funcionalidad que no corresponde a su rol.
Esta pantalla debe mostrar el siguiente mensaje:
“No posee permisos para acceder a esta sección.”
Además, debe incluir un enlace o botón para regresar al Home correspondiente al
rol del usuario autenticado.
El enlace debe comportarse de la siguiente manera:
● Si el usuario tiene rol Administrador, debe enviarlo al Home del administrador.
● Si el usuario tiene rol de Cajero, debe enviarlo al Home del cajero.
● Si el usuario tiene rol cliente, debe enviarlo al Home del cliente.
● Si el usuario no está autenticado, debe enviarlo a la pantalla de Login.
Implementación de seguridad
Todas las restricciones de acceso deben implementarse utilizando ASP.NET Identity
y los filtros de autorización de ASP.NET Core.
Los controladores y acciones protegidas deben utilizar autorización por roles, de
manera que cada funcionalidad solo pueda ser ejecutada por los usuarios
permitidos.
El menú principal debe mostrar únicamente las opciones disponibles para el rol del
usuario autenticado.
La opción Cerrar sesión debe estar disponible para todos los usuarios autenticados
de la aplicación web.
Al hacer clic en Cerrar sesión, el sistema debe cerrar la sesión activa y redirigir al
usuario a la pantalla de Login.
Usuarios creados por defecto
El sistema debe crear mediante seeding los roles principales de la aplicación web.

Los roles creados por defecto deben ser:
● Administrador.
● Cajero.
● Cliente.
Además, el sistema debe crear un usuario por defecto para cada rol:
Usuario por defecto Rol Estado inicial
Usuario administrador Administrador Activo
Usuario cajero Cajero Activo
Usuario cliente Cliente Activo
Estos usuarios por defecto deben crearse activos, ya que serán utilizados para
acceder inicialmente al sistema y probar las funcionalidades principales.
Los usuarios creados posteriormente desde la Gestión de usuarios deben quedar
inicialmente inactivos y deben completar el proceso de activación por correo
electrónico antes de poder iniciar sesión.
Reglas adicionales del módulo
● Todas las funcionalidades internas requieren autenticación.
● Los usuarios no autenticados deben ser redirigidos al Login.
● Los usuarios autenticados solo pueden acceder a las funcionalidades
correspondientes a su rol.
● El acceso directo por URL debe estar protegido mediante autorización por roles.
● El menú debe mostrar únicamente las opciones permitidas para el rol autenticado.
● La pantalla de Acceso denegado debe mostrarse cuando un usuario autenticado
intenta acceder a una sección no permitida.
● Los usuarios con rol Comercio no deben acceder a la aplicación web.
● Los usuarios creados por defecto mediante seeding deben estar activos.
● Los usuarios creados manualmente desde el sistema deben quedar inactivos

hasta completar el proceso de activación por correo.
Funcionalidades Administrador
Dashboard(Home)
Si el usuario autenticado tiene el rol Administrador, el sistema debe redirigir
automáticamente al Home del administrador luego de iniciar sesión correctamente.
Esta pantalla funcionará como el panel principal del administrador. Desde ella, el
usuario podrá acceder a los módulos administrativos del sistema y visualizar
indicadores generales relacionados con transacciones, pagos, clientes, productos
financieros y deuda promedio.
Menú principal del administrador
En el Home del administrador, el sistema debe mostrar un menú de navegación con
las opciones disponibles para este rol.
El menú debe contener las siguientes opciones:
Opción del menú Descripción
Home Envía al panel principal del administrador.
Gestión de usuarios Envía al mantenimiento de usuarios del sistema.
Envía al módulo de administración de
Gestión de préstamos
préstamos.
Envía al módulo de administración de tarjetas de
Gestión de tarjetas de crédito
crédito.
Envía al módulo de administración de cuentas
Gestión de cuentas de ahorro
de ahorro.
Cierra la sesión activa del usuario y lo redirige a
Cerrar sesión
la pantalla de Login.
Cada opción del menú debe estar disponible únicamente para usuarios con rol
Administrador.
Si un usuario que no tiene el rol Administrador intenta acceder directamente al

Home del administrador mediante la URL, el sistema debe impedir el acceso y
redirigirlo a la pantalla de Acceso denegado.
Indicadores generales del sistema
Además del menú principal, el Home del administrador debe mostrar una sección
con indicadores generales del sistema.
Estos indicadores deben permitir que el administrador visualiza un resumen del
estado actual de la plataforma y de las operaciones realizadas.
El sistema debe mostrar los siguientes indicadores:
Indicador Descripción
Cantidad total de transacciones registradas en el
Total de transacciones históricas
sistema desde su inicio.
Cantidad total de transacciones registradas durante
Transacciones del día
la fecha actual.
Cantidad total de pagos procesados correctamente
Total de pagos históricos
en todo el historial del sistema.
Cantidad de pagos procesados correctamente
Pagos del día
durante la fecha actual.
Cantidad de usuarios con rol cliente que se
Clientes activos
encuentran activos.
Cantidad de usuarios con rol cliente que se
Clientes inactivos
encuentran inactivos.
Suma total de cuentas de ahorro, préstamos y
Total de productos financieros
tarjetas de crédito activas asignadas a clientes.
Cantidad de préstamos activos asignados
Préstamos vigentes
actualmente a clientes.
Cantidad de tarjetas de crédito activas asociadas a
Tarjetas de crédito activas
clientes.

Cantidad total de cuentas de ahorro activas
Cuentas de ahorro activas
registradas en el sistema.
Monto promedio de deuda por Promedio de deuda calculado tomando en cuenta los
cliente clientes activos del sistema.
Descripción de los indicadores
Total de transacciones históricas
Representa la cantidad total de transacciones registradas en el sistema desde su
inicio.
Este indicador debe incluir operaciones como depósitos, retiros, transferencias entre
cuentas, transacciones a terceros, pagos a tarjetas de crédito, pagos a préstamos,
avances de efectivo y cualquier otra operación financiera que genere un registro de
transacciones dentro del sistema.
Transacciones del día
Representa la cantidad de transacciones registradas durante la fecha actual.
Para calcular este indicador, el sistema debe tomar como referencia la fecha de
creación o registro de la transacción.
Total de pagos históricos
Representa la cantidad total de pagos procesados correctamente en el sistema.
Para este indicador deben considerarse pagos las operaciones realizadas para
saldar o abonar una obligación financiera, como pagos a tarjetas de crédito y pagos
a préstamos.
No deben contarse como pagos los depósitos, retiros, transferencias entre cuentas
ni avances de efectivo, ya que estos representan movimientos de fondos, no pagos
de obligaciones financieras.
Pagos del día
Representa la cantidad de pagos procesados correctamente durante la fecha actual.
Debe calcularse usando las mismas reglas del indicador Total de pagos históricos,
pero tomando únicamente los pagos registrados en el día actual.

Clientes activos
Representa la cantidad de usuarios con rol cliente cuyo estado sea Activo.
Clientes inactivos
Representa la cantidad de usuarios con rol cliente cuyo estado sea inactivo.
Total de productos financieros
Representa la suma total de productos financieros activos asignados a clientes.
Para calcular este indicador, el sistema debe sumar:
● Cuentas de ahorro activas.
● Préstamos activos.
● Tarjetas de crédito activas.
No deben incluirse productos cancelados, completados o inactivos.
Préstamos vigentes
Representa la cantidad de préstamos activos asignados actualmente a clientes.
No deben incluirse préstamos completados.
Tarjetas de crédito activas
Representa la cantidad de tarjetas de crédito activas asociadas a clientes.
No deben incluirse tarjetas canceladas.
Cuentas de ahorro activas
Representa la cantidad total de cuentas de ahorro activas registradas en el sistema.
Deben incluirse tanto las cuentas principales como las cuentas secundarias.
No deben incluirse cuentas canceladas.
Monto promedio de deuda por cliente
Representa el promedio de deuda de los clientes activos del sistema.
La deuda total de un cliente debe calcularse sumando:
● El monto pendiente de sus préstamos activos.
● El monto adeudado en sus tarjetas de crédito activas.

La fórmula para calcular este indicador será:
Monto promedio de deuda por cliente = Deuda total de clientes activos / Cantidad
de clientes activos
Si no existen clientes activos en el sistema, el monto promedio de deuda debe
mostrarse como RD$0.00.
Reglas adicionales del módulo
● El Home del administrador solo debe estar disponible para usuarios con rol
Administrador.
● El menú principal debe mostrar únicamente opciones administrativas.
● La opción Cerrar sesión debe cerrar la sesión activa y redirigir al Login.
● Los indicadores deben calcularse usando información registrada en la base de
datos.
● Las transacciones del día y los pagos del día deben calcularse usando la fecha
actual del sistema.
● Los pagos históricos y pagos del día deben incluir únicamente pagos procesados
correctamente.
● Los productos financieros deben contarse únicamente si se encuentran activos.
● Las cuentas de ahorro activas deben incluir cuentas principales y secundarias.
● Los préstamos completados no deben contarse como préstamos vigentes.
● Las tarjetas canceladas no deben contarse como tarjetas activas.
● Si no existen clientes activos, el promedio de deuda debe mostrarse como
RD$0.00.
● El Home del administrador debe utilizar el mismo layout general de la aplicación
para mantener consistencia visual con los demás módulos.
Gestión de usuarios
Al ingresar a la opción Gestión de usuarios desde el menú principal del
administrador, el sistema debe enviar al usuario al módulo de administración de
usuarios.
Este módulo permitirá consultar, crear, editar, activar e inactivar usuarios de la
aplicación web.
Solo los usuarios con rol Administrador podrán acceder a esta funcionalidad. Si un
usuario con rol Cliente, Cajero o Comercio intenta acceder directamente a esta
pantalla mediante la URL, el sistema debe redirigirlo a la pantalla de Acceso
denegado.
Los usuarios con rol Comercio no deben mostrarse en este módulo, ya que este rol

será utilizado únicamente para la Web API y el procesador de pagos Hermes Pay.
Pantalla inicial de gestión de usuarios
En la pantalla inicial de Gestión de usuarios, el sistema debe mostrar un listado con
todos los usuarios registrados en la aplicación web, excluyendo los usuarios con rol
Comercio.
El listado debe estar ordenado desde el usuario más reciente hasta el más antiguo.
El listado debe estar paginado y debe mostrar un máximo de 20 usuarios por
página. Debajo del listado debe existir un componente de paginación que permita
navegar entre las páginas disponibles.
De cada usuario se debe mostrar la siguiente información:
Campo Descripción
Nombre de usuario Nombre utilizado por el usuario para iniciar sesión.
Cédula Número de documento de identidad del usuario.
Nombre Nombre del usuario.
Apellido Apellido del usuario.
Correo electrónico Correo registrado para el usuario.
Tipo de usuario Rol del usuario dentro de la aplicación web: Administrador,
Cajero o Cliente.
Estado Indica si el usuario se encuentra activo o inactivo.
Cada usuario listado debe tener las siguientes acciones:
Acción Descripción
Editar Permite modificar los datos del usuario.
Permite cambiar el estado del usuario de inactivo a activo. Solo
Activar
debe mostrarse si el usuario está inactivo.
Permite cambiar el estado del usuario de activo a inactivo. Solo
Inactivar
debe mostrarse si el usuario está activo.

Arriba del listado debe existir un botón con el texto Crear usuario.
Además, en la parte superior del listado debe existir un filtro de tipo select que
permite filtrar los usuarios por rol.
El filtro debe contener las siguientes opciones:
| Opción  |     | Descripción  |     |     |
| ------- | --- | ------------ | --- | --- |
Muestra todos los usuarios de la aplicación web,
Todos
excluyendo los usuarios con rol Comercio.
Administrador  Muestra únicamente usuarios con rol Administrador.
| Cajero   | Muestra únicamente usuarios con rol Cajero.   |     |     |     |
| -------- | --------------------------------------------- | --- | --- | --- |
| Cliente  | Muestra únicamente usuarios con rol cliente.  |     |     |     |
Crear usuario
Al hacer clic sobre el botón Crear usuario, el sistema debe enviar al administrador a
una pantalla de creación o mostrar un modal con el formulario correspondiente.
El formulario debe contener los siguientes campos:
| Campo     | Tipo de dato    | Requerido  |                        | Descripción  |
| --------- | --------------- | ---------- | ---------------------- | ------------ |
| Nombre    | Texto / string  | Sí         | Nombre del usuario.    |              |
| Apellido  | Texto / string  | Sí         | Apellido del usuario.  |              |
Cédula  Texto / string  Sí  Número de documento de identidad del
usuario.
Correo electrónico  Texto / string  Sí  Correo electrónico del usuario.
Nombre de usuario  Texto / string  Sí  Nombre que utilizará el usuario para inici
sesión.
| Contraseña  | Password /  | Sí  | Contraseña inicial del usuario.  |     |
| ----------- | ----------- | --- | -------------------------------- | --- |
string
Confirmar  Password /  Sí  Confirmación de la contraseña inicial.
| contraseña  | string  |     |     |     |
| ----------- | ------- | --- | --- | --- |

Campo Tipo de dato Requerido Descripción
Tipo de usuario Select / string Sí Rol que tendrá el usuario dentro de la
aplicación web.
Monto inicial Decimal / Solo para Monto inicial que será asignado a la cuen
number clientes de ahorro principal del cliente.
El campo Tipo de usuario debe permitir seleccionar únicamente los siguientes
valores:
Valor Descripción
Usuario con acceso a las funcionalidades
Administrador
administrativas.
Usuario con acceso a las funcionalidades operativas
Cajero
del cajero.
Cliente Usuario con acceso a las funcionalidades del cliente.
Si el administrador selecciona el tipo de usuario Cliente, el sistema debe mostrar el
campo Monto inicial.
Si el administrador selecciona Administrador o Cajero, el campo Monto inicial no
debe mostrarse.
Descripción de campos
Cédula
Representa el número de documento de identidad del usuario.
Este valor debe almacenarse como texto, no como número, para evitar pérdida de
ceros al inicio.
No debe existir más de un usuario registrado con la misma cédula.
Correo electrónico
Representa el correo donde el usuario recibirá notificaciones, enlaces de activación y
enlaces de restablecimiento de contraseña.
El correo debe tener un formato válido.

No debe existir más de un usuario registrado con el mismo correo electrónico.
Nombre de usuario
Representa el identificador que el usuario utilizará para iniciar sesión en la
aplicación web.
No debe existir más de un usuario registrado con el mismo nombre de usuario.
Contraseña y Confirmar contraseña
Estos campos permiten definir la contraseña inicial del usuario.
Ambos campos deben coincidir para permitir la creación del usuario.
Tipo de usuario
Define el rol que tendrá el usuario dentro de la aplicación web.
Una vez creado el usuario, el tipo de usuario no debe poder modificarse desde la
edición.
Monto inicial
Este campo sólo aplica cuando el usuario creado es de tipo Cliente.
Representa el saldo inicial que será asignado automáticamente a la cuenta de
ahorro principal del cliente.
Este campo debe permitir valores decimales mayores o iguales a cero.
Si el administrador no coloca un valor, el sistema debe asumir el monto inicial como
RD$0.00.
Validaciones al crear usuario
El formulario de creación de usuario debe cumplir las siguientes validaciones:
● El nombre es requerido.
● El apellido es requerido.
● La cédula es requerida.
● La cédula no debe estar registrada en otro usuario.
● El correo electrónico es requerido.
● El correo electrónico debe tener un formato válido.
● El correo electrónico no debe estar registrado en otro usuario.
● El nombre de usuario es requerido.

● El nombre de usuario no debe estar registrado en otro usuario.
● La contraseña es requerida.
● La confirmación de contraseña es requerida.
● La contraseña y la confirmación de contraseña deben coincidir.
● El tipo de usuario es requerido.
● El tipo de usuario debe ser Administrador, Cajero o Cliente.
● Si el usuario es Cliente, el monto inicial no puede ser negativo.
Si la cédula ya está registrada, el sistema debe mostrar el siguiente mensaje:
“Ya existe un usuario registrado con esta cédula.”
Si el correo electrónico ya está registrado, el sistema debe mostrar el siguiente
mensaje:
“Ya existe un usuario registrado con este correo electrónico.”
Si el nombre de usuario ya está registrado, el sistema debe mostrar el siguiente
mensaje:
“Ya existe un usuario registrado con este nombre de usuario.”
Si la contraseña y la confirmación de contraseña no coinciden, el sistema debe
mostrar el siguiente mensaje:
“La contraseña y la confirmación de contraseña deben coincidir.”
Si el monto inicial es menor que cero, el sistema debe mostrar el siguiente mensaje:
“El monto inicial no puede ser negativo.”
Si todas las validaciones son correctas, el sistema debe crear el usuario en estado
inactivo.
Creación automática de cuenta principal para clientes
Cuando el usuario creado sea de tipo Cliente, el sistema debe crear
automáticamente una cuenta de ahorro principal asociada a ese cliente.
La cuenta de ahorro principal debe contener la siguiente información:
Campo Descripción
Cliente Usuario cliente al que pertenece la cuenta.

Identificador único de 9 dígitos generado
Número de cuenta
automáticamente.
Valor indicado en el campo Monto inicial. Si no se
Balance inicial
indicó valor, debe ser RD$0.00.
Tipo de cuenta Principal.
Estado Activa.
Fecha de creación Fecha y hora en que fue creada la cuenta.
El número de cuenta debe cumplir las siguientes condiciones:
● Debe tener exactamente 9 dígitos.
● Debe ser único dentro del sistema.
● No debe existir previamente como número de cuenta de ahorro.
● No debe existir previamente como número identificador de préstamo.
● Debe almacenarse como texto para evitar pérdida de ceros iniciales.
Si el monto inicial de la cuenta es mayor que cero, el sistema debe registrar una
transacción inicial de tipo Crédito en el historial de la cuenta principal.
Esta transacción debe reflejar que el balance fue asignado al momento de la
apertura de la cuenta.
Si el monto inicial es RD$0.00, no es obligatorio registrar una transacción inicial.
Envío de correo de activación
Después de crear correctamente un usuario, el sistema debe enviar un correo
electrónico al correo registrado del usuario.
El correo debe contener un enlace de activación que permita activar la cuenta.
El enlace debe incluir un token único asociado al usuario creado.
El token de activación debe cumplir las siguientes condiciones:
Elemento Descripción
Relación Debe estar asociado al usuario creado.

Elemento Descripción
Uso Debe poder utilizarse una sola vez.
Al utilizarse correctamente, se debe activar la cuenta del
Resultado
usuario.
El correo puede tener un contenido como el siguiente:
Asunto: Activación de cuenta
Hola [Nombre del usuario],
Su cuenta ha sido creada correctamente en Artemis Banking.
Para activar su usuario, haga clic en el siguiente enlace:
[ENLACE_DE_ACTIVACION]
Si usted no esperaba la creación de esta cuenta, ignore este mensaje.
Si ocurre un error al enviar el correo de activación, el sistema debe mostrar el
siguiente mensaje:
“No fue posible enviar el correo de activación. Intente nuevamente más tarde.”
Activar o inactivar usuario
Dentro del listado de usuarios, el administrador podrá activar o inactivar usuarios.
Si el usuario está activo, el sistema debe mostrar la acción "Inactivar".
Si el usuario está inactivo, el sistema debe mostrar la acción Activar.
Al hacer clic en cualquiera de estas acciones, el sistema debe mostrar una pantalla
o modal de confirmación antes de aplicar el cambio.
Si el administrador intenta inactivar un usuario activo, el sistema debe mostrar el
siguiente mensaje de confirmación:
“¿Está seguro que desea inactivar este usuario?”
Si el administrador intenta activar un usuario inactivo, el sistema debe mostrar el
siguiente mensaje de confirmación:
“¿Está seguro que desea activar este usuario?”

Debajo del mensaje deben existir dos botones:
Botón Descripción
Cancelar Regresa al listado de usuarios sin aplicar cambios.
Aceptar Aplica el cambio de estado del usuario.
Si el administrador confirma la acción, el sistema debe actualizar el estado del
usuario.
Si el administrador cancela la acción, no debe realizarse ninguna modificación.
El administrador autenticado no debe poder modificar el estado de su propia
cuenta.
Si intenta hacerlo, el sistema debe mostrar el siguiente mensaje:
“No puede modificar el estado de su propia cuenta.”
La inactivación de un usuario debe impedir que dicho usuario pueda iniciar sesión.
La inactivación de un usuario no debe eliminar sus productos financieros,
transacciones ni historial registrado.
Editar usuario
Cada usuario listado debe tener una acción Editar.
Al hacer clic sobre esta acción, el sistema debe enviar al administrador a una
pantalla de edición o mostrar un modal con el formulario correspondiente.
El formulario de edición debe cargar los datos actuales del usuario seleccionado.
El administrador autenticado no debe poder editar su propia cuenta desde este
módulo.
Si intenta hacerlo, el sistema debe mostrar el siguiente mensaje:
“No puede editar su propia cuenta desde este módulo.”
Para usuarios con rol Administrador o Cajero, el formulario debe permitir modificar
los siguientes campos:

| Campo     | Tipo de dato    | Requerido  |                        | Descripción  |
| --------- | --------------- | ---------- | ---------------------- | ------------ |
| Nombre    | Texto / string  | Sí         | Nombre del usuario.    |              |
| Apellido  | Texto / string  | Sí         | Apellido del usuario.  |              |
Número de documento
| Cédula  | Texto / string  | Sí  |     |     |
| ------- | --------------- | --- | --- | --- |
de identidad del usuario.
Correo electrónico del
| Correo electrónico  | Texto / string  | Sí  |     |     |
| ------------------- | --------------- | --- | --- | --- |
usuario.
Nombre utilizado para
| Nombre de usuario  | Texto / string  | Sí  |     |     |
| ------------------ | --------------- | --- | --- | --- |
iniciar sesión.
Nueva contraseña del
|             | Password /  |     | usuario. Solo se modifica  |     |
| ----------- | ----------- | --- | -------------------------- | --- |
| Contraseña  |             | No  |                            |     |
|             | string      |     | si se completa este        |     |
campo.
Solo si se
| Confirmar  | Password /  |     | Confirmación de la nueva  |     |
| ---------- | ----------- | --- | ------------------------- | --- |
escribe
| contraseña  | string  |     | contraseña.  |     |
| ----------- | ------- | --- | ------------ | --- |
contraseña
Para usuarios con rol Cliente, el formulario debe permitir modificar los mismos
campos anteriores y además debe mostrar el campo Monto adicional.
| Campo            | Tipo de dato      | Requerido  |     | Descripción     |
| ---------------- | ----------------- | ---------- | --- | --------------- |
| Monto adicional  | Decimal / number  | No         |     | Monto que será  |
sumado al balance
actual de la cuenta
de ahorro principal
del cliente.
El tipo de usuario no debe poder modificarse durante la edición.
Validaciones al editar usuario
El formulario de edición de usuario debe cumplir las siguientes validaciones:

● El usuario a editar debe existir.
● El nombre es requerido.
● El apellido es requerido.
● La cédula es requerida.
● La cédula no debe pertenecer a otro usuario.
● El correo electrónico es requerido.
● El correo electrónico debe tener un formato válido.
● El correo electrónico no debe pertenecer a otro usuario.
● El nombre de usuario es requerido.
● El nombre de usuario no debe pertenecer a otro usuario.
● Si se escribe una nueva contraseña, debe escribirse la confirmación.
● Si se escribe una nueva contraseña, la contraseña y la confirmación deben
coincidir.
● El monto adicional no puede ser negativo.
Si el usuario seleccionado no existe, el sistema debe mostrar el siguiente mensaje:
“El usuario seleccionado no existe.”
Si la cédula pertenece a otro usuario, el sistema debe mostrar el siguiente mensaje:
“Ya existe otro usuario registrado con esta cédula.”
Si el correo electrónico pertenece a otro usuario, el sistema debe mostrar el
siguiente mensaje:
“Ya existe otro usuario registrado con este correo electrónico.”
Si el nombre de usuario pertenece a otro usuario, el sistema debe mostrar el
siguiente mensaje:
“Ya existe otro usuario registrado con este nombre de usuario.”
Si se escribe una nueva contraseña y no se confirma, el sistema debe mostrar el
siguiente mensaje:
“Debe confirmar la nueva contraseña.”
Si la contraseña y la confirmación no coinciden, el sistema debe mostrar el siguiente
mensaje:
“La contraseña y la confirmación de contraseña deben coincidir.”
Si el campo Contraseña se deja vacío durante la edición, el sistema no debe
modificar la contraseña actual del usuario.

Aplicación del monto adicional
El campo Monto adicional solo debe estar disponible para usuarios con rol Cliente.
Si el administrador introduce un monto adicional mayor que cero, el sistema debe
sumar ese valor al balance actual de la cuenta de ahorro principal del cliente.
Ejemplo:
Balance actual Monto adicional Nuevo balance
RD$5,000.00 RD$12,000.00 RD$17,000.00
Cuando se aplique un monto adicional, el sistema debe registrar una transacción de
tipo Crédito en el historial de la cuenta principal del cliente.
Esta transacción debe reflejar que el aumento de balance fue realizado por una
acción administrativa.
Si el monto adicional es RD$0.00 o se deja vacío, el sistema no debe modificar el
balance de la cuenta ni registrar transacciones.
Reglas adicionales del módulo
● Solo los usuarios con rol Administrador pueden acceder a la Gestión de usuarios.
● Los usuarios con rol Comercio no deben mostrarse ni administrarse desde este
módulo.
● El listado debe mostrar un máximo de 20 usuarios por página.
● Los usuarios deben mostrarse desde el más reciente hasta el más antiguo.
● El administrador debe poder filtrar el listado por tipo de usuario.
● Todo usuario creado desde este módulo debe quedar inicialmente inactivo.
● Todo usuario creado desde este módulo debe recibir un correo con enlace de
activación.
● Al crear un usuario de tipo Cliente, el sistema debe crear automáticamente una
cuenta de ahorro principal.
● Todo cliente debe tener una cuenta de ahorro principal.
● El número de cuenta principal debe tener 9 dígitos y no puede repetirse como
cuenta ni como préstamo.
● El tipo de usuario no debe poder modificarse después de creado.
● El administrador autenticado no puede editar ni cambiar el estado de su propia
cuenta.
● Inactivar un usuario impide su inicio de sesión, pero no elimina su historial ni
productos financieros.

● El monto adicional solo aplica para clientes y debe sumarse a la cuenta principal
del cliente.
● Todo monto inicial o monto adicional mayor que cero debe registrarse como una
transacción de tipo Crédito.
Gestión de préstamos
Al ingresar a la opción Gestión de préstamos desde el menú principal del
administrador, el sistema debe enviar al usuario al módulo de administración de
préstamos.
Este módulo permitirá consultar los préstamos registrados en el sistema, asignar
nuevos préstamos a clientes, visualizar la tabla de amortización de un préstamo y
modificar la tasa de interés anual de préstamos existentes.
Solo los usuarios con rol Administrador podrán acceder a esta funcionalidad. Si un
usuario con rol Cliente, Cajero o Comercio intenta acceder directamente a esta
pantalla mediante la URL, el sistema debe redirigirlo a la pantalla de Acceso
denegado.
Pantalla inicial de gestión de préstamos
En la pantalla inicial de Gestión de préstamos, el sistema debe mostrar un listado
con los préstamos registrados en el sistema.
Por defecto, el listado debe mostrar los préstamos activos, ordenados desde el más
reciente hasta el más antiguo.
El listado debe estar paginado y debe mostrar un máximo de 20 préstamos por
página. Debajo del listado debe existir un componente de paginación que permita
navegar entre las páginas disponibles.
De cada préstamo se debe mostrar la siguiente información:
Campo Descripción
Número de préstamo Identificador único del préstamo.
Nombre y apellido del cliente al que pertenece el
Cliente
préstamo.
Capital aprobado Monto aprobado al momento de asignar el préstamo.

Campo Descripción
Cantidad total de cuotas Total de cuotas generadas para el préstamo.
Cuotas pagadas Cantidad de cuotas saldadas completamente.
Monto pendiente Monto total pendiente por pagar del préstamo.
Tasa de interés anual Porcentaje de interés anual aplicado al préstamo.
Plazo Duración del préstamo expresada en meses.
Estado del préstamo Indica si el préstamo está activo o completado.
Estado del cliente Indica si el cliente se encuentra al día o en mora.
Cada préstamo listado debe tener las siguientes acciones:
Acción Descripción
Ver detalles Permite visualizar la tabla de amortización del préstamo.
Editar Permite modificar la tasa de interés anual del préstamo.
Arriba del listado debe existir un botón con el texto Asignar préstamo.
Búsqueda y filtros
En la parte superior del listado deben existir herramientas de búsqueda y filtrado.
El sistema debe incluir un campo de búsqueda que permita ingresar la cédula de un
cliente para consultar los préstamos asociados a ese usuario.
También debe existir un filtro de estado que permita consultar préstamos según su
estado.
El filtro debe contener las siguientes opciones:
Opción Descripción
Activos Muestra únicamente préstamos activos.

Opción Descripción
Completados Muestra únicamente préstamos completados.
Todos Muestra préstamos activos y completados.
Si el administrador busca por cédula y no selecciona un estado específico, el
sistema debe mostrar todos los préstamos del cliente, colocando primero los
préstamos activos y luego los completados. Dentro de cada grupo, los préstamos
deben mostrarse desde el más reciente hasta el más antiguo.
Si no existe ningún cliente con la cédula ingresada, el sistema debe mostrar el
siguiente mensaje:
“No existe un cliente registrado con esta cédula.”
Si el cliente existe, pero no tiene préstamos registrados, el sistema debe mostrar el
siguiente mensaje:
“Este cliente no tiene préstamos registrados.”
Asignar préstamo
Al hacer clic sobre el botón Asignar préstamo, el sistema debe enviar al
administrador a una pantalla donde pueda seleccionar el cliente al que se le
asignará el préstamo.
Esta pantalla debe mostrar un listado con todos los clientes activos que no tengan
actualmente un préstamo activo.
Un cliente solo puede tener un préstamo activo a la vez.
En la parte superior de esta pantalla debe mostrarse el monto promedio de deuda
de los clientes activos del sistema.
También debe existir un campo de búsqueda que permita localizar clientes por
cédula.
De cada cliente se debe mostrar la siguiente información:
Campo Descripción
Cédula Número de documento de identidad del cliente.

Campo Descripción
Nombre y apellido Nombre completo del cliente.
Correo electrónico Correo registrado del cliente.
Monto total de deuda Deuda actual del cliente, tomando en cuenta
préstamos activos y tarjetas de crédito activas.
Cada cliente listado debe tener un campo de selección tipo radio button.
El sistema solo debe permitir seleccionar un cliente a la vez.
Debajo del listado debe existir un botón con el texto Siguiente paso.
También debe existir un botón con el texto Volver atrás, el cual debe regresar al
listado principal de préstamos.
Validaciones al seleccionar cliente
Antes de avanzar al formulario de asignación del préstamo, el sistema debe validar:
● Debe seleccionarse un cliente.
● El cliente seleccionado debe existir.
● El cliente debe estar activo.
● El cliente no debe tener un préstamo activo actualmente.
Si el administrador intenta continuar sin seleccionar un cliente, el sistema debe
mostrar el siguiente mensaje:
“Debe seleccionar un cliente para continuar.”
Si el cliente seleccionado ya tiene un préstamo activo, el sistema debe mostrar el
siguiente mensaje:
“Este cliente ya tiene un préstamo activo asignado.”
Si todas las validaciones son correctas, el sistema debe enviar al administrador al
formulario de configuración del préstamo.
Formulario de asignación de préstamo
Luego de seleccionar un cliente válido, el sistema debe mostrar un formulario para
configurar el préstamo que será asignado.

El formulario debe contener los siguientes campos:
Campo Tipo de dato Requerido Descripción
Plazo del Select / entero Sí Cantidad de meses en que será
préstamo pagado el préstamo.
Monto a prestar Decimal / Sí Capital que será aprobado al
number cliente.
Tasa de interés Decimal / Sí Porcentaje anual de interés
anual number aplicado al préstamo.
El campo Plazo del préstamo debe permitir únicamente las siguientes opciones:
● 6 meses.
● 12 meses.
● 18 meses.
● 24 meses.
● 30 meses.
● 36 meses.
● 42 meses.
● 48 meses.
● 54 meses.
● 60 meses.
Al final del formulario deben existir dos botones:
Botón Descripción
Regresa al listado de clientes para seleccionar otro
Volver atrás
cliente.
Valida los datos y continúa con el proceso de asignación
Asignar préstamo
del préstamo.
Descripción de campos
Plazo del préstamo
Representa la duración del préstamo en meses.
El plazo mínimo permitido debe ser de 6 meses y el plazo máximo permitido debe

ser de 60 meses.
Solo se permiten intervalos de 6 meses.
Monto a prestar
Representa el capital que será aprobado al cliente.
Este monto será desembolsado en la cuenta de ahorro principal del cliente luego de
crear correctamente el préstamo.
Tasa de interés anual
Representa el porcentaje anual de interés aplicado al préstamo.
Esta tasa se utilizará para calcular la cuota mensual mediante el sistema francés de
amortización.
Validaciones al asignar préstamo
El formulario de asignación de préstamo debe cumplir las siguientes validaciones:
● El cliente seleccionado es requerido.
● El cliente seleccionado debe existir.
● El cliente debe estar activo.
● El cliente no debe tener un préstamo activo.
● El plazo del préstamo es requerido.
● El plazo del préstamo debe ser uno de los valores permitidos.
● El monto a prestar es requerido.
● El monto a prestar debe ser mayor que cero.
● La tasa de interés anual es requerida.
● La tasa de interés anual debe ser mayor o igual a cero.
Si el plazo seleccionado no es válido, el sistema debe mostrar el siguiente mensaje:
“El plazo seleccionado no es válido.”
Si el monto a prestar es menor o igual a cero, el sistema debe mostrar el siguiente
mensaje:
“El monto a prestar debe ser mayor que cero.”
Si la tasa de interés anual es menor que cero, el sistema debe mostrar el siguiente
mensaje:
“La tasa de interés anual no puede ser negativa.”

Evaluación de riesgo del cliente
Después de validar los datos del préstamo, el sistema debe evaluar el nivel de
riesgo del cliente antes de registrar el préstamo.
Para esta evaluación, el sistema debe calcular la deuda promedio de los clientes
activos.
La deuda promedio debe calcularse tomando la deuda total de todos los clientes
activos y dividiéndola entre la cantidad de clientes activos.
La deuda total de un cliente debe incluir:
● Monto pendiente de préstamos activos.
● Monto adeudado en tarjetas de crédito activas.
Para evaluar el nuevo préstamo, el sistema debe calcular la deuda proyectada del
cliente.
La deuda proyectada debe ser igual a:
Deuda actual del cliente + total a pagar del nuevo préstamo.
El total a pagar del nuevo préstamo debe calcularse sumando todas las cuotas
generadas por la tabla de amortización.
Si el cliente ya tiene una deuda actual superior a la deuda promedio del sistema, el
sistema debe enviarlo a una pantalla de advertencia.
En ese caso, debe mostrarse el siguiente mensaje:
“Este cliente se considera de alto riesgo, ya que su deuda actual supera el promedio
del sistema.”
Si el cliente no supera actualmente la deuda promedio, pero al agregar el nuevo
préstamo su deuda proyectada supera la deuda promedio del sistema, el sistema
debe enviarlo a una pantalla de advertencia.
En ese caso, debe mostrarse el siguiente mensaje:
“Asignar este préstamo convertirá al cliente en un cliente de alto riesgo, ya que su
deuda superará el umbral promedio del sistema.”
En la pantalla de advertencia deben existir dos botones:

Botón Descripción
Cancelar Cancela la asignación y regresa al listado principal de
préstamos.
Confirmar asignación Confirma que el administrador desea continuar con la
asignación del préstamo.
Si el administrador cancela, el préstamo no debe crearse.
Si el administrador confirma, el sistema debe continuar con la creación del
préstamo.
Si la deuda del cliente se mantiene por debajo o igual a la deuda promedio, el
sistema debe crear el préstamo sin mostrar advertencia.
Si no existen clientes activos para calcular la deuda promedio, el sistema debe
tomar la deuda promedio como RD$0.00.
Registro del préstamo
Una vez validados los datos y aprobada la advertencia de riesgo si aplica, el sistema
debe registrar el préstamo.
El préstamo debe guardar como mínimo la siguiente información:
Campo Descripción
Cliente Cliente al que se le asigna el préstamo.
Número de préstamo Identificador único de 9 dígitos.
Monto aprobado Capital aprobado al cliente.
Plazo Duración del préstamo en meses.
Tasa de interés anual Porcentaje de interés anual aplicado.
Usuario administrador Administrador responsable de la asignación.
Estado El préstamo debe crearse en estado activo.
Fecha de creación Fecha y hora en que se registró el préstamo.

El número de préstamo debe cumplir las siguientes condiciones:
● Debe tener exactamente 9 dígitos.
● Debe ser único dentro del sistema.
● No debe existir previamente como número de préstamo.
● No debe existir previamente como número de cuenta de ahorro.
● Debe almacenarse como texto para evitar pérdida de ceros iniciales.
Cálculo de la cuota del préstamo
Después de registrar el préstamo, el sistema debe generar automáticamente la
tabla de amortización correspondiente.
Para calcular las cuotas, se debe utilizar el sistema francés de amortización, también
conocido como sistema de cuota fija.
En este sistema, la cuota mensual se mantiene constante durante el plazo del
préstamo, aunque la proporción entre interés y capital varía en cada período.
La fórmula para calcular la cuota mensual debe ser:
C = P * [r * (1 + r)^n] / [(1 + r)^n - 1]
Donde:
C = cuota mensual fija.
P = monto del préstamo o capital aprobado.
r = tasa de interés mensual.
n = cantidad total de cuotas del préstamo.
La tasa de interés mensual debe calcularse de la siguiente forma:
r = (tasa de interés anual / 100) / 12
Si la tasa de interés anual es 0%, la cuota mensual debe calcularse dividiendo el
capital entre la cantidad de cuotas:
C = P / n
Los valores monetarios deben calcularse y almacenarse con precisión decimal,
redondeando a dos decimales cuando corresponda.
Generación de tabla de amortización
Luego de calcular la cuota mensual, el sistema debe generar una cuota por cada
mes del plazo seleccionado.

La primera cuota tendrá como fecha de vencimiento el mismo día del mes siguiente
a la fecha en que fue creado el préstamo.
Ejemplo:
Si el préstamo se registra el 5 de julio de 2025, la primera cuota tendrá fecha de
vencimiento el 5 de agosto de 2025.
La segunda cuota tendrá fecha de vencimiento el 5 de septiembre de 2025.
Este proceso debe continuar hasta completar la cantidad total de cuotas del
préstamo.
Cada cuota de la tabla de amortización debe contener la siguiente información:
Campo Descripción
Préstamo Préstamo al que pertenece la cuota.
Número de cuota Posición de la cuota dentro del préstamo.
Fecha de vencimiento Fecha en que debe pagarse la cuota.
Valor de la cuota Monto total que debe pagar el cliente en esa cuota.
Monto de interés Parte de la cuota correspondiente a intereses.
Monto de capital Parte de la cuota correspondiente a amortización del
capital.
Saldo pendiente de la Monto pendiente por pagar de esa cuota.
cuota
Estado de pago Indica si la cuota está pendiente, parcialmente
pagada o pagada.
Indicador de atraso Indica si la cuota está atrasada.
Al momento de crear la tabla de amortización, todas las cuotas deben quedar en
estado pendiente y sin indicador de atraso.
Control automático de cuotas atrasadas
El sistema debe implementar un proceso automático diario utilizando Azure
Functions.
Este proceso debe revisar las cuotas pendientes de todos los préstamos activos.

Una cuota debe marcarse como atrasada si cumple las siguientes condiciones:
● La fecha de vencimiento ya pasó.
● La cuota no ha sido pagada completamente.
Si una cuota atrasada es pagada posteriormente, el sistema debe actualizarla para
que ya no aparezca como atrasada.
Un préstamo debe considerarse en mora si tiene al menos una cuota vencida y no
pagada completamente.
Un préstamo debe considerarse al día si no tiene cuotas atrasadas.
Desembolso del préstamo
Una vez creado el préstamo y generada su tabla de amortización, el sistema debe
desembolsar el monto aprobado en la cuenta de ahorro principal del cliente.
El monto aprobado debe sumarse automáticamente al balance actual de la cuenta
principal.
Ejemplo:
Si el cliente tiene RD$5,000.00 en su cuenta principal y se aprueba un préstamo de
RD$100,000.00, el nuevo balance de la cuenta será de RD$105,000.00.
El desembolso debe registrarse como una transacción de tipo Crédito en el historial
de la cuenta principal del cliente.
La transacción debe reflejar que el origen del dinero corresponde al préstamo
aprobado.
Si el cliente no tiene una cuenta de ahorro principal activa, el sistema no debe
completar la asignación del préstamo y debe mostrar el siguiente mensaje:
“El cliente no tiene una cuenta de ahorro principal activa para recibir el desembolso
del préstamo.”
Correo de aprobación del préstamo
Después de crear correctamente el préstamo, generar la tabla de amortización y
desembolsar el monto aprobado, el sistema debe enviar un correo electrónico al
cliente notificando la aprobación del préstamo.
El correo debe incluir como mínimo la siguiente información:

● Monto aprobado.
● Plazo del préstamo.
● Tasa de interés anual aplicada.
● Valor de la cuota mensual.
● Número de préstamo.
El correo puede tener un contenido como el siguiente:
Asunto: Préstamo aprobado
Hola [Nombre del cliente],
Su préstamo ha sido aprobado correctamente.
Número de préstamo: [Número del préstamo]
Monto aprobado: RD$[Monto aprobado]
Plazo: [Plazo] meses
Tasa de interés anual: [Tasa]%
Cuota mensual: RD$[Cuota mensual]
El monto aprobado ha sido depositado en su cuenta de ahorro principal.
Si ocurre un error al enviar el correo, el préstamo no debe eliminarse ni revertirse. El
sistema debe registrar el error y mostrar un mensaje informativo al administrador.
Mensaje sugerido:
“El préstamo fue creado correctamente, pero no fue posible enviar el correo de
notificación.”
Después de completar el proceso, el sistema debe redirigir al administrador al
listado principal de préstamos.
Ver detalles del préstamo
Desde el listado principal de préstamos, cada préstamo debe tener una acción Ver
detalles.
Al hacer clic sobre esta acción, el sistema debe enviar al administrador a una
pantalla donde se muestre la tabla de amortización correspondiente al préstamo
seleccionado.
En esta pantalla se debe mostrar la información general del préstamo:

Campo Descripción
Número de préstamo Identificador del préstamo.
Cliente Nombre y apellido del cliente.
Monto aprobado Capital aprobado.
Tasa de interés anual Porcentaje anual aplicado.
Plazo Duración del préstamo en meses.
Estado del préstamo Estado actual del préstamo.
Por cada cuota de la tabla de amortización se debe mostrar la siguiente información:
Campo Descripción
Número de cuota Posición de la cuota dentro del préstamo.
Fecha de vencimiento Fecha en que debe pagarse la cuota.
Valor de la cuota Monto total de la cuota.
Saldo pendiente Monto pendiente por pagar de la cuota.
Estado de pago Pendiente, parcialmente pagada o pagada.
Indicador de atraso Indica si la cuota está atrasada.
Además de la tabla, esta pantalla debe incluir un botón con el texto Volver atrás.
Al hacer clic sobre este botón, el sistema debe regresar al listado principal de
préstamos.
Editar tasa de interés del préstamo
Desde el listado principal de préstamos, cada préstamo debe tener una acción
Editar.
Al hacer clic sobre esta acción, el sistema debe enviar al administrador a una
pantalla donde pueda modificar la tasa de interés anual del préstamo seleccionado.
El formulario debe contener los siguientes campos:

Campo Tipo de dato Requerido Descripción
Tasa de interés Decimal / Sí Nueva tasa de interés anual que
anual number será aplicada al préstamo.
El campo Tasa de interés anual debe mostrar por defecto la tasa actual del
préstamo.
Al final del formulario deben existir dos botones:
Botón Descripción
Regresa al listado principal de préstamos sin aplicar
Volver atrás
cambios.
Modificar tasa Valida la nueva tasa y aplica el cambio al préstamo.
Validaciones al editar tasa de interés
El formulario de edición de tasa debe cumplir las siguientes validaciones:
● El préstamo seleccionado debe existir.
● El préstamo debe estar activo.
● La tasa de interés anual es requerida.
● La tasa de interés anual no puede ser negativa.
● Debe existir al menos una cuota futura pendiente para poder recalcular el
préstamo.
Si el préstamo seleccionado no existe, el sistema debe mostrar el siguiente
mensaje:
“El préstamo seleccionado no existe.”
Si el préstamo no está activo, el sistema debe mostrar el siguiente mensaje:
“Solo se puede modificar la tasa de interés de préstamos activos.”
Si la tasa de interés anual es negativa, el sistema debe mostrar el siguiente
mensaje:
“La tasa de interés anual no puede ser negativa.”
Si el préstamo no tiene cuotas futuras pendientes, el sistema debe mostrar el
siguiente mensaje:

“No existen cuotas futuras pendientes para recalcular.”
Recalcular cuotas futuras
Al modificar la tasa de interés anual, el sistema debe recalcular únicamente las
cuotas futuras que estén pendientes y cuya fecha de vencimiento sea posterior a la
fecha actual.
No deben modificarse:
● Cuotas ya pagadas.
● Cuotas vencidas.
● Cuotas parcialmente pagadas.
● Cuotas con fecha de vencimiento igual o anterior a la fecha actual.
El cambio de tasa sólo debe aplicarse a pagos futuros.
Una vez aplicada la nueva tasa, el sistema debe actualizar el préstamo y recalcular
las cuotas futuras correspondientes.
Correo de actualización de tasa
Después de aplicar correctamente la nueva tasa de interés y recalcular las cuotas
futuras, el sistema debe enviar un correo electrónico al cliente notificando el cambio.
El correo debe incluir como mínimo la siguiente información:
● Número de préstamo.
● Nueva tasa de interés anual.
● Nuevo valor de la próxima cuota pendiente.
● Fecha de vencimiento de la próxima cuota pendiente.
El correo puede tener un contenido como el siguiente:
Asunto: Actualización de tasa de interés de préstamo
Hola [Nombre del cliente],
La tasa de interés de su préstamo [Número de préstamo] ha sido actualizada.
Nueva tasa de interés anual: [Tasa]%
Nuevo valor de la próxima cuota: RD$[Monto de cuota]
Fecha de vencimiento de la próxima cuota: [Fecha]
Esta modificación aplica únicamente a las cuotas futuras pendientes.

Reglas adicionales del módulo
● Solo los usuarios con rol Administrador pueden acceder a la Gestión de
préstamos.
● Por defecto, el listado debe mostrar préstamos activos.
● El listado debe estar paginado y mostrar un máximo de 20 préstamos por página.
● Los préstamos deben mostrarse desde el más reciente hasta el más antiguo.
● El administrador debe poder buscar préstamos por cédula del cliente.
● El administrador debe poder filtrar préstamos por estado.
● Un cliente solo puede tener un préstamo activo a la vez.
● Solo se puede asignar préstamos a clientes activos.
● El préstamo debe crearse en estado activo.
● El número de préstamo debe tener 9 dígitos y no puede repetirse como préstamo
ni como cuenta de ahorro.
● El monto aprobado debe ser desembolsado en la cuenta de ahorro principal
activa del cliente.
● Todo desembolso de préstamo debe registrarse como una transacción de tipo
Crédito.
● La tabla de amortización debe generarse automáticamente al crear el préstamo.
● Las cuotas deben calcularse usando el sistema francés de amortización.
● Una cuota puede estar pendiente, parcialmente pagada o pagada.
● Una cuota debe marcarse como atrasada si su fecha de vencimiento pasó y no ha
sido pagada completamente.
● Un préstamo debe considerarse en mora si tiene al menos una cuota atrasada.
● Solo las cuotas futuras pendientes pueden calcularse al modificar la tasa de
interés.
● Las cuotas pagadas, vencidas o parcialmente pagadas no deben modificarse al
cambiar la tasa.
● El sistema debe enviar un correo al cliente cuando se apruebe un préstamo.
● El sistema debe enviar un correo al cliente cuando se modifique la tasa de interés
del préstamo.
Gestión de tarjetas de crédito
Al ingresar a la opción Gestión de tarjetas de crédito desde el menú principal del
administrador, el sistema debe enviar al usuario al módulo de administración de
tarjetas de crédito.
Este módulo permitirá consultar tarjetas de crédito registradas, asignar nuevas
tarjetas a clientes, visualizar los consumos asociados a una tarjeta, modificar el
límite aprobado y cancelar tarjetas que no tengan deuda pendiente.
Solo los usuarios con rol Administrador podrán acceder a esta funcionalidad. Si un

usuario con rol Cliente, Cajero o Comercio intenta acceder directamente a esta
pantalla mediante la URL, el sistema debe redirigirlo a la pantalla de Acceso
denegado.
Pantalla inicial de gestión de tarjetas de crédito
En la pantalla inicial de Gestión de tarjetas de crédito, el sistema debe mostrar un
listado con las tarjetas de crédito registradas en el sistema.
Por defecto, el listado debe mostrar las tarjetas activas, ordenadas desde la más
reciente hasta la más antigua.
El listado debe estar paginado y debe mostrar un máximo de 20 tarjetas por página.
Debajo del listado debe existir un componente de paginación que permita navegar
entre las páginas disponibles.
De cada tarjeta se debe mostrar la siguiente información:
Campo Descripción
Número identificador de la tarjeta de crédito. Debe
Número de tarjeta mostrarse enmascarado, dejando visibles únicamente
los últimos 4 dígitos.
Nombre y apellido del cliente al que está asignada la
Cliente
tarjeta.
Límite de crédito Monto máximo aprobado para la tarjeta.
Fecha de expiración Fecha de expiración de la tarjeta en formato MM/AA.
Monto adeudado Monto total pendiente de pago en la tarjeta.
Estado Indica si la tarjeta está activa o cancelada.
Cada tarjeta listada debe tener las siguientes acciones:
Acción Descripción

Ver detalles Permite visualizar los consumos registrados para la tarjeta.
Editar Permite modificar el límite de crédito de la tarjeta.
Cancelar Permite iniciar el proceso de cancelación de la tarjeta. Solo
debe estar disponible si la tarjeta está activa.
Búsqueda y filtros
En la parte superior del listado deben existir herramientas de búsqueda y filtrado.
El sistema debe incluir un campo de búsqueda que permita ingresar la cédula de un
cliente para consultar las tarjetas de crédito asociadas a ese usuario.
También debe existir un filtro de estado que permita consultar tarjetas según su
estado.
El filtro debe contener las siguientes opciones:
Opción Descripción
Activas Muestra únicamente tarjetas activas.
Canceladas Muestra únicamente tarjetas canceladas.
Todas Muestra tarjetas activas y canceladas.
Si el administrador busca por cédula y no selecciona un estado específico, el
sistema debe mostrar todas las tarjetas del cliente, colocando primero las tarjetas
activas y luego las canceladas. Dentro de cada grupo, las tarjetas deben mostrarse
desde la más reciente hasta la más antigua.
Si no existe ningún cliente con la cédula ingresada, el sistema debe mostrar el
siguiente mensaje:
“No existe un cliente registrado con esta cédula.”
Si el cliente existe, pero no tiene tarjetas de crédito registradas, el sistema debe
mostrar el siguiente mensaje:
“Este cliente no tiene tarjetas de crédito registradas.”
Asignar tarjeta de crédito

En la parte superior del listado debe existir un botón con el texto Asignar tarjeta de
crédito.
Al hacer clic sobre este botón, el sistema debe enviar al administrador a una
pantalla donde pueda seleccionar el cliente al que se le asignará la tarjeta.
Esta pantalla debe mostrar un listado con todos los clientes activos del sistema.
En la parte superior de esta pantalla debe mostrarse el monto promedio de deuda
de los clientes activos del sistema.
También debe existir un campo de búsqueda que permita localizar clientes por
cédula.
De cada cliente se debe mostrar la siguiente información:
Campo Descripción
Cédula Número de documento de identidad del cliente.
Nombre y apellido Nombre completo del cliente.
Correo electrónico Correo registrado del cliente.
Monto total de Deuda actual del cliente, tomando en cuenta préstamos
deuda activos y tarjetas de crédito activas.
Cada cliente listado debe tener un campo de selección tipo radio button.
El sistema solo debe permitir seleccionar un cliente a la vez.
Debajo del listado debe existir un botón con el texto Siguiente paso.
También debe existir un botón con el texto Volver atrás, el cual debe regresar al
listado principal de tarjetas de crédito.
Validaciones al seleccionar cliente
Antes de avanzar al formulario de asignación de la tarjeta, el sistema debe validar:
● Debe seleccionarse un cliente.
● El cliente seleccionado debe existir.
● El cliente debe estar activo.
Si el administrador intenta continuar sin seleccionar un cliente, el sistema debe

mostrar el siguiente mensaje:
“Debe seleccionar un cliente para continuar.”
Si el cliente seleccionado no está activo, el sistema debe mostrar el siguiente
mensaje:
“Solo se puede asignar tarjetas de crédito a clientes activos.”
Si todas las validaciones son correctas, el sistema debe enviar al administrador al
formulario de configuración de la tarjeta de crédito.
Formulario de asignación de tarjeta de crédito
Luego de seleccionar un cliente válido, el sistema debe mostrar un formulario para
configurar la tarjeta de crédito que será asignada.
El formulario debe contener el siguiente campo:
Campo Tipo de dato Requerido Descripción
Límite de crédito Decimal / Sí Monto máximo aprobado para
number la tarjeta de crédito.
Al final del formulario deben existir dos botones:
Botón Descripción
Volver atrás Regresa al listado de clientes para seleccionar otro cliente.
Asignar Valida los datos y confirma la asignación de la tarjeta.
Descripción del campo
Límite de crédito
Representa el monto máximo que el cliente podrá consumir con la tarjeta de crédito.
Este valor no debe registrarse como deuda inicial. La deuda de la tarjeta debe iniciar
en RD$0.00 y solo debe aumentar cuando se registren consumos aprobados o
avances de efectivo.
Validaciones al asignar tarjeta de crédito

El formulario de asignación de tarjeta debe cumplir las siguientes validaciones:
● El cliente seleccionado es requerido.
● El cliente seleccionado debe existir.
● El cliente debe estar activo.
● El límite de crédito es requerido.
● El límite de crédito debe ser mayor que cero.
Si el límite de crédito es menor o igual a cero, el sistema debe mostrar el siguiente
mensaje:
“El límite de crédito debe ser mayor que cero.”
Si todas las validaciones son correctas, el sistema debe registrar la nueva tarjeta de
crédito.
Registro de la tarjeta de crédito
Una vez validados los datos, el sistema debe crear una nueva tarjeta de crédito para
el cliente seleccionado.
La tarjeta debe guardar como mínimo la siguiente información:
Campo Descripción
Cliente Cliente al que se le asigna la tarjeta.
Identificador único de 16 dígitos generado
Número de tarjeta
automáticamente.
Límite de crédito Monto máximo aprobado para la tarjeta.
Monto adeudado Debe iniciar en RD$0.00.
Fecha de expiración Debe calcularse sumando 3 años a la fecha actual.
Código de seguridad de 3 dígitos generado
CVC
automáticamente.
Usuario administrador Administrador responsable de la asignación.
Estado La tarjeta debe crearse en estado activa.

Fecha de creación Fecha y hora en que se registró la tarjeta.
El número de tarjeta debe cumplir las siguientes condiciones:
● Debe tener exactamente 16 dígitos.
● Debe ser único dentro del sistema.
● No debe existir previamente en otra tarjeta registrada.
● Debe almacenarse como texto para evitar pérdida de ceros iniciales.
La fecha de expiración debe almacenarse en formato mes/año, utilizando el formato
MM/AA.
El CVC debe cumplir las siguientes condiciones:
● Debe tener exactamente 3 dígitos.
● Debe generarse automáticamente.
● No debe almacenarse en texto plano.
● Debe almacenarse como hash utilizando SHA-256.
Después de crear correctamente la tarjeta, el sistema debe redirigir al administrador
al listado principal de tarjetas de crédito.
Correo de asignación de tarjeta
Después de registrar correctamente la tarjeta de crédito, el sistema debe enviar un
correo electrónico al cliente notificando que se le ha asignado una nueva tarjeta.
El correo debe incluir como mínimo la siguiente información:
● Últimos 4 dígitos de la tarjeta.
● Límite de crédito aprobado.
● Fecha de expiración.
● Fecha de asignación.
El correo no debe incluir el CVC completo ni el número completo de la tarjeta.
El correo puede tener un contenido como el siguiente:
Asunto: Nueva tarjeta de crédito asignada
Hola [Nombre del cliente],
Se ha asignado una nueva tarjeta de crédito a su cuenta.
Tarjeta terminada en: [Últimos 4 dígitos]

Límite aprobado: RD$[Límite]
Fecha de expiración: [MM/AA]
Por seguridad, no comparta la información de su tarjeta con terceros.
Si ocurre un error al enviar el correo, la tarjeta no debe eliminarse ni revertirse. El
sistema debe registrar el error y mostrar un mensaje informativo al administrador.
Mensaje sugerido:
“La tarjeta fue creada correctamente, pero no fue posible enviar el correo de
notificación.”
Ver detalles de la tarjeta
Desde el listado principal de tarjetas de crédito, cada tarjeta debe tener una acción
Ver detalles.
Al hacer clic sobre esta acción, el sistema debe enviar al administrador a una
pantalla donde se muestre el listado de consumos registrados para la tarjeta
seleccionada.
Los consumos deben mostrarse desde el más reciente hasta el más antiguo.
Por cada consumo se debe mostrar la siguiente información:
Campo Descripción
Fecha del consumo Fecha y hora en que se intentó realizar el consumo.
Monto consumido Monto del consumo realizado o intentado.
Comercio Nombre del comercio donde se realizó el consumo. Si
corresponde a un avance de efectivo, debe mostrarse el
texto AVANCE.
Estado del consumo Indica si el consumo fue aprobado o rechazado.
El estado del consumo debe mostrarse de la siguiente manera:
● APROBADO: cuando el consumo fue autorizado porque la tarjeta tenía crédito
disponible suficiente.
● RECHAZADO: cuando el consumo fue denegado por falta de crédito disponible o

porque la tarjeta no estaba activa.
Además del listado de consumos, esta pantalla debe incluir un botón con el texto
Volver atrás.
Al hacer clic sobre este botón, el sistema debe regresar al listado principal de
tarjetas de crédito.
Editar tarjeta de crédito
Desde el listado principal de tarjetas de crédito, cada tarjeta activa debe tener una
acción Editar.
Al hacer clic sobre esta acción, el sistema debe enviar al administrador a una
pantalla donde pueda modificar el límite de crédito de la tarjeta seleccionada.
El formulario debe contener el siguiente campo:
Campo Tipo de dato Requerido Descripción
Decimal / Nuevo límite de crédito que tendrá la
Límite de la tarjeta Sí
number tarjeta.
El campo Límite de la tarjeta debe mostrarse precargado con el valor actual
registrado para esa tarjeta.
Al final del formulario deben existir dos botones:
Botón Descripción
Volver atrás Regresa al listado principal de tarjetas sin aplicar cambios.
Guardar cambios Valida el nuevo límite y aplica la modificación.
Validaciones al editar tarjeta de crédito
El formulario de edición debe cumplir las siguientes validaciones:
● La tarjeta seleccionada debe existir.
● La tarjeta debe estar activa.
● El límite de la tarjeta es requerido.
● El límite de la tarjeta debe ser mayor que cero.
● El nuevo límite no puede ser menor que el monto actualmente adeudado en la
tarjeta.

Si la tarjeta seleccionada no existe, el sistema debe mostrar el siguiente mensaje:
“La tarjeta seleccionada no existe.”
Si la tarjeta está cancelada, el sistema debe mostrar el siguiente mensaje:
“No se puede modificar una tarjeta cancelada.”
Si el límite ingresado es menor o igual a cero, el sistema debe mostrar el siguiente
mensaje:
“El límite de la tarjeta debe ser mayor que cero.”
Si el nuevo límite es menor que la deuda actual de la tarjeta, el sistema debe
mostrar el siguiente mensaje:
“El límite de la tarjeta no puede ser inferior al monto adeudado actualmente.”
Si todas las validaciones son correctas, el sistema debe actualizar el límite de
crédito de la tarjeta.
El sistema debe permitir aumentar o disminuir el límite, siempre que el nuevo límite
sea mayor o igual al monto adeudado actualmente.
Correo de modificación de límite
Después de actualizar correctamente el límite de crédito, el sistema debe enviar un
correo electrónico al cliente notificando el cambio.
El correo debe incluir como mínimo la siguiente información:
● Últimos 4 dígitos de la tarjeta.
● Nuevo límite aprobado.
● Fecha de modificación.
El correo puede tener un contenido como el siguiente:
Asunto: Modificación de límite de tarjeta
Hola [Nombre del cliente],
El límite de su tarjeta de crédito terminada en [Últimos 4 dígitos] ha sido
actualizado.
Nuevo límite aprobado: RD$[Nuevo límite]
Si usted no reconoce esta modificación, comuníquese con la entidad bancaria.

Si ocurre un error al enviar el correo, el cambio de límite no debe revertirse. El
sistema debe registrar el error y mostrar un mensaje informativo al administrador.
Mensaje sugerido:
“El límite fue actualizado correctamente, pero no fue posible enviar el correo de
notificación.”
Cancelar tarjeta de crédito
Desde el listado principal de tarjetas de crédito, cada tarjeta activa debe tener una
acción Cancelar.
Al hacer clic sobre esta acción, el sistema debe enviar al administrador a una
pantalla de confirmación.
La pantalla debe mostrar el siguiente mensaje:
“¿Está seguro que desea cancelar la tarjeta [XXXX]?”
Donde [XXXX] corresponde a los últimos cuatro dígitos del número de la tarjeta.
Debajo del mensaje deben mostrarse dos botones:
Botón Descripción
Cancelar Regresa al listado de tarjetas sin realizar ninguna acción.
Aceptar Válida si la tarjeta puede ser cancelada.
Al presionar el botón Aceptar, el sistema debe verificar si la tarjeta tiene deuda
pendiente.
Si el saldo adeudado es mayor que cero, el sistema no debe cancelar la tarjeta y
debe mostrar el siguiente mensaje:
“Para cancelar esta tarjeta, el cliente debe saldar la totalidad de la deuda
pendiente.”
Si la tarjeta no tiene deuda pendiente, el sistema debe actualizar su estado a
cancelado.
Una vez cancelada la tarjeta:
● Cualquier intento de consumo con dicha tarjeta debe ser rechazado

automáticamente.
● Cualquier intento de avance de efectivo con dicha tarjeta debe ser rechazado
automáticamente.
● La tarjeta no debe aparecer en el listado de productos activos del cliente.
● La tarjeta cancelada debe mantenerse en el historial administrativo del sistema.
● La tarjeta no debe eliminarse físicamente de la base de datos.
Reglas adicionales del módulo
● Solo los usuarios con rol Administrador pueden acceder a la Gestión de tarjetas
de crédito.
● Por defecto, el listado debe mostrar tarjetas activas.
● El listado debe estar paginado y mostrar un máximo de 20 tarjetas por página.
● Las tarjetas deben mostrarse desde la más reciente hasta la más antigua.
● El administrador debe poder buscar tarjetas por cédula del cliente.
● El administrador debe poder filtrar tarjetas por estado.
● Solo se puede asignar tarjetas de crédito a clientes activos.
● El límite de crédito debe ser mayor que cero.
● La deuda inicial de una tarjeta nueva debe ser RD$0.00.
● El número de tarjeta debe tener 16 dígitos y ser único en el sistema.
● El CVC debe tener 3 dígitos y no debe almacenarse en texto plano.
● La fecha de expiración debe generarse automáticamente sumando 3 años a la
fecha actual.
● El número completo de tarjeta no debe mostrarse en listados generales ni
correos electrónicos.
● El sistema debe mostrar únicamente los últimos 4 dígitos cuando sea necesario
identificar la tarjeta.
● El nuevo límite de una tarjeta no puede ser inferior al monto adeudado
actualmente.
● Una tarjeta solo puede cancelarse si no tiene deuda pendiente.
● Cancelar una tarjeta no debe eliminar su historial de consumos.
● Una tarjeta cancelada no puede generar consumos, avances de efectivo ni
aparecer como producto activo del cliente.
● El sistema debe enviar un correo al cliente cuando se le asigne una tarjeta nueva.
● El sistema debe enviar un correo al cliente cuando se modifique el límite de su
tarjeta.
Gestión de cuentas de ahorro

Al ingresar a la opción Gestión de cuentas de ahorro desde el menú principal del
administrador, el sistema debe enviar al usuario al módulo de administración de
cuentas de ahorro.
Este módulo permitirá consultar las cuentas de ahorro registradas en el sistema,
asignar nuevas cuentas secundarias a clientes, visualizar las transacciones
asociadas a una cuenta y cancelar cuentas secundarias.
Solo los usuarios con rol Administrador podrán acceder a esta funcionalidad. Si un
usuario con rol Cliente, Cajero o Comercio intenta acceder directamente a esta
pantalla mediante la URL, el sistema debe redirigirlo a la pantalla de Acceso
denegado.
Pantalla inicial de gestión de cuentas de ahorro
En la pantalla inicial de Gestión de cuentas de ahorro, el sistema debe mostrar un
listado con las cuentas de ahorro registradas en el sistema.
Por defecto, el listado debe mostrar las cuentas activas, tanto principales como
secundarias, ordenadas desde la más reciente hasta la más antigua.
El listado debe estar paginado y debe mostrar un máximo de 20 cuentas por
página. Debajo del listado debe existir un componente de paginación que permita
navegar entre las páginas disponibles.
De cada cuenta se debe mostrar la siguiente información:
Campo Descripción
Número de cuenta Identificador único de 9 dígitos de la cuenta de ahorro.
Cliente Nombre y apellido del cliente al que pertenece la cuenta.
Balance Monto disponible actualmente en la cuenta.
Tipo de cuenta Indica si la cuenta es Principal o Secundaria.
Estado Indica si la cuenta está Activa o Cancelada.
Cada cuenta listada debe tener las siguientes acciones:

Acción Descripción
Permite visualizar las transacciones registradas para la
Ver detalles
cuenta.
Permite cancelar una cuenta secundaria. Solo debe
Cancelar
mostrarse si la cuenta está activa y es secundaria.
Las cuentas principales no deben poder cancelarse desde este módulo.
Búsqueda y filtros
En la parte superior del listado deben existir herramientas de búsqueda y filtrado.
El sistema debe incluir un campo de búsqueda que permita ingresar la cédula de un
cliente para consultar las cuentas de ahorro asociadas a ese usuario.
También deben existir filtros que permitan consultar las cuentas según su estado y
su tipo.
El filtro por estado debe contener las siguientes opciones:
Opción Descripción
Activas Muestra únicamente cuentas activas.
Canceladas Muestra únicamente cuentas canceladas.
Todas Muestra cuentas activas y canceladas.
El filtro por tipo debe contener las siguientes opciones:
Opción Descripción
Todas Muestra cuentas principales y secundarias.
Principal Muestra únicamente cuentas principales.
Secundaria Muestra únicamente cuentas secundarias.
Si el administrador busca por cédula y no selecciona un estado específico, el
sistema debe mostrar todas las cuentas del cliente, colocando primero las cuentas
activas y luego las canceladas. Dentro de cada grupo, las cuentas deben mostrarse

desde la más reciente hasta la más antigua.
Si no existe ningún cliente con la cédula ingresada, el sistema debe mostrar el
siguiente mensaje:
“No existe un cliente registrado con esta cédula.”
Si el cliente existe, pero no tiene cuentas de ahorro registradas, el sistema debe
mostrar el siguiente mensaje:
“Este cliente no tiene cuentas de ahorro registradas.”
Asignar cuenta de ahorro
En la parte superior del listado debe existir un botón con el texto Asignar cuenta de
ahorro.
Al hacer clic sobre este botón, el sistema debe enviar al administrador a una
pantalla donde pueda seleccionar el cliente al que se le asignará una nueva cuenta
de ahorro secundaria.
Esta pantalla debe mostrar un listado con todos los clientes activos del sistema.
En la parte superior de esta pantalla debe existir un campo de búsqueda que
permita localizar clientes por cédula.
De cada cliente se debe mostrar la siguiente información:
Campo Descripción
Cédula Número de documento de identidad del cliente.
Nombre y apellido Nombre completo del cliente.
Correo electrónico Correo registrado del cliente.
Deuda actual del cliente, tomando en cuenta
Monto total de deuda
préstamos activos y tarjetas de crédito activas.
Cada cliente listado debe tener un campo de selección tipo radio button.
El sistema solo debe permitir seleccionar un cliente a la vez.
Debajo del listado debe existir un botón con el texto Siguiente paso.

También debe existir un botón con el texto Volver atrás, el cual debe regresar al
listado principal de cuentas de ahorro.
Validaciones al seleccionar cliente
Antes de avanzar al formulario de asignación de cuenta de ahorro, el sistema debe
validar:
● Debe seleccionarse un cliente.
● El cliente seleccionado debe existir.
● El cliente debe estar activo.
● El cliente debe tener una cuenta de ahorro principal activa.
Si el administrador intenta continuar sin seleccionar un cliente, el sistema debe
mostrar el siguiente mensaje:
“Debe seleccionar un cliente para continuar.”
Si el cliente seleccionado no está activo, el sistema debe mostrar el siguiente
mensaje:
“Solo se puede asignar cuentas de ahorro a clientes activos.”
Si el cliente no tiene una cuenta de ahorro principal activa, el sistema debe mostrar
el siguiente mensaje:
“El cliente debe tener una cuenta de ahorro principal activa antes de asignarle una
cuenta secundaria.”
Si todas las validaciones son correctas, el sistema debe enviar al administrador al
formulario de configuración de la cuenta de ahorro secundaria.
Formulario de asignación de cuenta de ahorro secundaria
Luego de seleccionar un cliente válido, el sistema debe mostrar un formulario para
configurar la cuenta de ahorro secundaria que será asignada.
El formulario debe contener el siguiente campo:
Campo Tipo de dato Requerido Descripción

Monto inicial que tendrá la
Decimal /
Balance inicial Sí cuenta de ahorro secundaria.
number
Puede ser RD$0.00.
Al final del formulario deben existir dos botones:
Botón Descripción
Volver atrás Regresa al listado de clientes para seleccionar otro cliente.
Asignar Valida los datos y confirma la creación de la cuenta de
ahorro secundaria.
Descripción del campo
Balance inicial
Representa el monto inicial que tendrá la cuenta de ahorro secundaria al momento
de ser creada.
Este valor puede ser RD$0.00, pero no puede ser negativo.
Si el balance inicial es mayor que cero, el sistema debe registrar una transacción
inicial de tipo CRÉDITO en el historial de la cuenta.
Validaciones al asignar cuenta de ahorro
El formulario de asignación de cuenta de ahorro debe cumplir las siguientes
validaciones:
● El cliente seleccionado es requerido.
● El cliente seleccionado debe existir.
● El cliente debe estar activo.
● El balance inicial es requerido.
● El balance inicial debe ser mayor o igual a cero.
Si el balance inicial es menor que cero, el sistema debe mostrar el siguiente
mensaje:
“El balance inicial no puede ser negativo.”
Si todas las validaciones son correctas, el sistema debe registrar la nueva cuenta de
ahorro secundaria.

Registro de la cuenta de ahorro secundaria
Una vez validados los datos, el sistema debe crear una nueva cuenta de ahorro
secundaria para el cliente seleccionado.
La cuenta debe guardar como mínimo la siguiente información:
Campo Descripción
Cliente Cliente al que se le asigna la cuenta.
Identificador único de 9 dígitos generado
Número de cuenta
automáticamente.
Balance inicial Monto indicado en el formulario de asignación.
Tipo de cuenta La cuenta debe crearse como Secundaria.
Usuario administrador Administrador responsable de la asignación.
Estado La cuenta debe crearse en estado Activa.
Fecha de creación Fecha y hora en que se registró la cuenta.
El número de cuenta debe cumplir las siguientes condiciones:
● Debe tener exactamente 9 dígitos.
● Debe ser único dentro del sistema.
● No debe existir previamente como número de cuenta de ahorro.
● No debe existir previamente como número de préstamo.
● Debe almacenarse como texto para evitar pérdida de ceros iniciales.
Si el balance inicial es mayor que RD$0.00, el sistema debe registrar una
transacción inicial de tipo CRÉDITO.
Esta transacción debe reflejar que el balance fue asignado al momento de crear la
cuenta.
Después de crear correctamente la cuenta, el sistema debe redirigir al administrador
al listado principal de cuentas de ahorro.
Ver detalles de la cuenta

Desde el listado principal de cuentas de ahorro, cada cuenta debe tener una acción
Ver detalles.
Al hacer clic sobre esta acción, el sistema debe enviar al administrador a una
pantalla donde se muestre el listado de transacciones registradas para la cuenta
seleccionada.
Las transacciones deben mostrarse desde la más reciente hasta la más antigua.
Por cada transacción se debe mostrar la siguiente información:
Campo Descripción
Fecha de la transacción Fecha y hora en que se registró la transacción.
Monto Monto de la transacción.
Tipo de transacción Indica si la transacción fue DÉBITO o CRÉDITO.
Beneficiario Destino de la transacción.
Origen Fuente desde la cual se generó la transacción.
Estado Indica si la transacción fue APROBADA o
RECHAZADA.
Descripción de campos de la transacción
Tipo de transacción
El sistema debe utilizar el valor DÉBITO cuando la transacción represente una
salida o disminución de fondos desde la cuenta.
El sistema debe utilizar el valor CRÉDITO cuando la transacción represente un
ingreso o aumento de fondos en la cuenta.
Beneficiario
Representa el destino de la transacción.
Si la transacción corresponde a una transferencia hacia una cuenta de ahorro, ya sea
una transacción express o una transacción a beneficiario, debe mostrarse el número
de cuenta beneficiaria.
Si la transacción corresponde a un retiro realizado por cajero, debe mostrarse el

texto RETIRO.
Si la transacción corresponde a un pago de tarjeta de crédito, deben mostrarse los
últimos 4 dígitos de la tarjeta a la que se aplicó el pago.
Si la transacción corresponde a un pago de préstamo, debe mostrarse el número
identificador del préstamo al que fue destinado el pago.
Origen
Representa la fuente desde la cual se generó la transacción.
Si la transacción corresponde a una transferencia entre cuentas, transacción express
o transacción a beneficiario, debe mostrarse el número de cuenta desde la cual se
descontaron los fondos.
Si la transacción corresponde a un retiro realizado por cajero, debe mostrarse el
número de cuenta desde la cual se realizó el retiro.
Si la transacción corresponde a un avance de efectivo desde una tarjeta de crédito,
deben mostrarse los últimos 4 dígitos de la tarjeta utilizada.
Si la transacción corresponde al desembolso de un préstamo, debe mostrarse el
número identificador del préstamo como origen.
Si la transacción corresponde a un depósito realizado por cajero, debe mostrarse el
texto DEPÓSITO.
Estado de la transacción
La transacción debe mostrarse como APROBADA cuando fue autorizada y aplicada
correctamente.
La transacción debe mostrarse como RECHAZADA cuando el intento fue denegado,
por ejemplo, por fondos insuficientes, cuenta cancelada, producto inválido o
cualquier otra restricción de negocio.
Además del listado de transacciones, esta pantalla debe incluir un botón con el
texto Volver atrás.
Al hacer clic sobre este botón, el sistema debe regresar al listado principal de
cuentas de ahorro.
Cancelar cuenta de ahorro

Desde el listado principal de cuentas de ahorro, solo las cuentas secundarias activas
deben tener disponible la acción Cancelar.
Las cuentas principales no deben mostrar la acción Cancelar y no deben poder
cancelarse mediante acceso directo por URL.
Al hacer clic sobre la acción Cancelar, el sistema debe enviar al administrador a una
pantalla de confirmación.
La pantalla debe mostrar el siguiente mensaje:
“¿Está seguro que desea cancelar la cuenta [XXXXXXXXX]?”
Donde [XXXXXXXXX] corresponde al número identificador de 9 dígitos de la cuenta.
Debajo del mensaje deben mostrarse dos botones:
Botón Descripción
Regresa al listado de cuentas sin realizar ninguna
Cancelar
acción.
Valida y confirma la cancelación de la cuenta de
Aceptar
ahorro.
Al presionar el botón Aceptar, el sistema debe verificar que la cuenta pueda ser
cancelada.
El sistema debe validar:
● La cuenta debe existir.
● La cuenta debe estar activa.
● La cuenta debe ser secundaria.
● El cliente debe tener una cuenta principal activa para recibir el balance
disponible.
Si la cuenta no existe, el sistema debe mostrar el siguiente mensaje:
“La cuenta seleccionada no existe.”
Si la cuenta ya está cancelada, el sistema debe mostrar el siguiente mensaje:
“La cuenta seleccionada ya se encuentra cancelada.”
Si la cuenta es principal, el sistema debe mostrar el siguiente mensaje:

“Las cuentas principales no pueden ser canceladas.”
Si el cliente no tiene una cuenta principal activa, el sistema debe mostrar el
siguiente mensaje:
“No es posible cancelar la cuenta porque el cliente no tiene una cuenta principal
activa para recibir los fondos.”
Transferencia de balance al cancelar una cuenta
Si la cuenta secundaria tiene un balance disponible mayor que RD$0.00, el sistema
debe transferir automáticamente ese monto a la cuenta de ahorro principal del
mismo cliente.
Esta operación debe realizarse antes de cambiar el estado de la cuenta secundaria a
Cancelada.
El sistema debe registrar las transacciones correspondientes:
● Una transacción de tipo DÉBITO en la cuenta secundaria que será cancelada.
● Una transacción de tipo CRÉDITO en la cuenta principal que recibirá los fondos.
Luego de transferir el balance, la cuenta secundaria debe quedar con balance
RD$0.00.
Si la cuenta secundaria no tiene balance disponible, el sistema debe cancelar la
cuenta sin realizar transferencia de fondos.
Una vez finalizado el proceso, la cuenta secundaria debe actualizarse al estado
Cancelada.
A partir de ese momento:
● Cualquier intento de transacción utilizando esa cuenta debe ser rechazado
automáticamente.
● Cualquier intento de pago utilizando esa cuenta debe ser rechazado
automáticamente.
● La cuenta no debe aparecer en el listado de productos activos del cliente.
● La cuenta cancelada debe mantenerse en el historial administrativo del sistema.
● La cuenta cancelada no debe eliminarse físicamente de la base de datos.
Reglas adicionales del módulo

● Solo los usuarios con rol Administrador pueden acceder a la Gestión de cuentas
de ahorro.
● Por defecto, el listado debe mostrar cuentas activas.
● El listado debe estar paginado y mostrar un máximo de 20 cuentas por página.
● Las cuentas deben mostrarse desde la más reciente hasta la más antigua.
● El administrador debe poder buscar cuentas por cédula del cliente.
● El administrador debe poder filtrar cuentas por estado y por tipo.
● Las cuentas principales se crean automáticamente al crear un usuario de tipo
Cliente.
● Desde este módulo solo se deben asignar cuentas de ahorro secundarias.
● Solo se puede asignar cuentas de ahorro a clientes activos.
● Toda cuenta de ahorro secundaria debe crearse en estado Activa.
● El número de cuenta debe tener 9 dígitos y no puede repetirse como cuenta ni
como préstamo.
● El balance inicial puede ser RD$0.00, pero no puede ser negativo.
● Todo balance inicial mayor que cero debe registrarse como una transacción de
tipo CRÉDITO.
● Las cuentas principales no pueden cancelarse.
● Sólo pueden cancelarse cuentas secundarias activas.
● Si una cuenta secundaria tiene balance al momento de cancelarse, dicho balance
debe transferirse a la cuenta principal del cliente.
● Al cancelar una cuenta secundaria con balance, debe registrar un DÉBITO en la
cuenta secundaria y un CRÉDITO en la cuenta principal.
● Una cuenta cancelada no puede generar nuevas transacciones ni pagos.
● Una cuenta cancelada no debe aparecer como producto activo del cliente.
● Cancelar una cuenta no debe eliminar su historial de transacciones.
Funcionalidades cliente
Home(Listado de productos)
Luego de iniciar sesión correctamente con un usuario de tipo Cliente, el sistema
debe redirigir automáticamente al Home del cliente.
Esta pantalla funcionará como el panel principal del cliente dentro de la aplicación
web. Desde ella, el cliente podrá visualizar sus productos financieros activos y
acceder a las funcionalidades disponibles para su rol.
Solo los usuarios con rol cliente podrán acceder a esta pantalla. Si un usuario con
rol Administrador, Cajero o Comercio intenta acceder directamente mediante la
URL, el sistema debe redirigirlo a la pantalla de Acceso denegado.
Menú principal del cliente

En el Home del cliente, el sistema debe mostrar un menú de navegación con las
opciones disponibles para este rol.
El menú debe contener las siguientes opciones:
Opción del menú Descripción
Home Envía al panel principal del cliente, donde se
muestran sus productos financieros activos.
Transacciones Menú principal para acceder a las operaciones
transaccionales del cliente.
Transacciones - Express Permite realizar una transferencia express hacia una
cuenta de ahorro.
Transacciones - Tarjeta Permite realizar pagos a tarjetas de crédito.
de crédito
Transacciones - Préstamo Permite realizar pagos a préstamos.
Transacciones - Permite realizar transacciones hacia beneficiarios
Beneficiarios registrados.
Beneficiarios Permite administrar los beneficios del cliente.
Avance de efectivo Permite realizar avances desde una tarjeta de crédito
hacia una cuenta de ahorro propia.
Transferencia Permite realizar transferencias entre cuentas de
ahorro propias.
Cerrar sesión Cierra la sesión activa del usuario y lo redirige a la
pantalla de Login.
La opción Cerrar sesión debe eliminar la sesión activa del cliente y redirigirlo a la
pantalla de Login.
Listado de productos financieros
Además del menú principal, el Home del cliente debe mostrar los productos
financieros activos asociados al cliente autenticado.
El sistema debe mostrar hasta tres secciones:

● Listado de cuentas de ahorro activas.
● Listado de préstamos activos.
● Listado de tarjetas de crédito activas.
La sección de cuentas de ahorro debe mostrarse siempre que el cliente tenga al
menos una cuenta activa.
La sección de préstamos solo debe mostrarse si el cliente tiene préstamos activos.
La sección de tarjetas de crédito solo debe mostrarse si el cliente tiene tarjetas de
crédito activas.
Si el cliente no tiene préstamos activos, no debe mostrarse la sección de préstamos.
Si el cliente no tiene tarjetas de crédito activas, no debe mostrarse la sección de
tarjetas de crédito.
Si por alguna razón el cliente no tiene ningún producto financiero activo, el sistema
debe mostrar el siguiente mensaje:
“No posee productos financieros activos.”
Listado de cuentas de ahorro
En esta sección se deben mostrar todas las cuentas de ahorro activas del cliente
autenticado.
De cada cuenta de ahorro se debe mostrar la siguiente información:
Campo Descripción
Número de cuenta Número identificador de 9 dígitos de la cuenta de
ahorro.
Balance actual Monto disponible actualmente en la cuenta.
Tipo de cuenta Indica si la cuenta es Principal o Secundaria.
La cuenta principal debe mostrarse siempre en primer lugar.
Las cuentas secundarias deben mostrarse después de la cuenta principal,
ordenadas de mayor a menor balance.
Cada cuenta debe tener un botón con el texto Ver detalles.
Ver detalles de una cuenta de ahorro

Al hacer clic sobre el botón Ver detalles de una cuenta de ahorro, el sistema debe
enviar al cliente a una pantalla donde se muestre el listado de transacciones
registradas para esa cuenta.
El cliente solo debe poder visualizar transacciones de cuentas que le pertenezcan.
Las transacciones deben mostrarse desde la más reciente hasta la más antigua.
Por cada transacción se debe mostrar la siguiente información:
Campo Descripción
Fecha de la transacción Fecha y hora en que se registró la transacción.
Monto Monto de la transacción.
Tipo de transacción Indica si la transacción fue DÉBITO o CRÉDITO.
Beneficiario Destino de la transacción.
Origen Fuente desde la cual se generó la transacción.
Estado Indica si la transacción fue APROBADA o
RECHAZADA.
Descripción de campos de la transacción
Tipo de transacción
El sistema debe utilizar el valor DÉBITO cuando la transacción represente una
salida o disminución de fondos desde la cuenta del cliente.
El sistema debe utilizar el valor CRÉDITO cuando la transacción represente un
ingreso o aumento de fondos en la cuenta del cliente.
Beneficiario
Representa el destino de la transacción.
Si la transacción corresponde a una transferencia hacia una cuenta de ahorro, ya sea
una transacción express o una transacción hacia beneficiario, debe mostrarse el
número de cuenta beneficiaria.
Si la transacción corresponde a un pago de tarjeta de crédito, deben mostrarse los
últimos 4 dígitos de la tarjeta a la que se aplicó el pago.
Si la transacción corresponde a un pago de préstamo, debe mostrarse el número

identificador del préstamo al que fue destinado el pago.
Si la transacción corresponde a un retiro realizado por cajero, debe mostrarse el
texto RETIRO.
Origen
Representa la fuente desde la cual se generó la transacción.
Si la transacción corresponde a una transferencia entre cuentas propias, transacción
express o transacción hacia beneficiario, debe mostrarse el número de cuenta desde
la cual se descontaron los fondos.
Si la transacción corresponde a un retiro realizado por cajero, debe mostrarse el
número de cuenta desde la cual se realizó el retiro.
Si la transacción corresponde a un avance de efectivo realizado desde una tarjeta de
crédito, deben mostrarse los últimos 4 dígitos de la tarjeta utilizada.
Si la transacción corresponde al desembolso de un préstamo, debe mostrarse el
número identificador del préstamo como origen.
Si la transacción corresponde a un depósito realizado por cajero, debe mostrarse el
texto DEPÓSITO.
Estado de la transacción
La transacción debe mostrarse como APROBADA cuando fue procesada
correctamente y aplicada al balance correspondiente.
La transacción debe mostrarse como RECHAZADA cuando el intento de transacción
no pudo ser aplicado, por ejemplo, por fondos insuficientes, cuenta cancelada,
producto inválido o cualquier otra restricción de negocio.
Además del listado de transacciones, esta pantalla debe incluir un botón con el
texto Volver atrás.
Al hacer clic sobre este botón, el sistema debe regresar al Home del cliente.
Listado de préstamos
En esta sección se deben mostrar todos los préstamos activos del cliente
autenticado.
Esta sección no debe mostrarse si el cliente no tiene préstamos activos.

De cada préstamo se debe mostrar la siguiente información:
Campo Descripción
Número de préstamo Número identificador del préstamo.
Capital aprobado Monto total del capital prestado.
Cantidad total de cuotas Total de cuotas generadas para el préstamo.
Cuotas pagadas Cantidad de cuotas que ya fueron saldadas.
Monto pendiente Monto total pendiente por pagar del préstamo.
Tasa de interés anual Porcentaje de interés anual aplicado al préstamo.
Plazo Duración del préstamo expresada en meses.
Estado del préstamo Indica si el préstamo está al día o en mora.
Un préstamo debe considerarse en mora si tiene al menos una cuota vencida que no
ha sido pagada completamente.
Un préstamo debe considerarse al día si no tiene cuotas atrasadas.
Cada préstamo debe tener un botón con el texto Ver detalles.
Ver detalles de un préstamo
Al hacer clic sobre el botón Ver detalles de un préstamo, el sistema debe enviar al
cliente a una pantalla donde se muestre la tabla de amortización correspondiente a
ese préstamo.
El cliente solo debe poder visualizar los préstamos que le pertenezcan.
Por cada cuota de la tabla de amortización se debe mostrar la siguiente información:
Campo Descripción
Fecha de pago Fecha de vencimiento de la cuota.
Valor de la cuota Monto total que debe pagarse en esa cuota.
Indica si la cuota está pendiente, parcialmente
Estado de pago
pagada o pagada.

Campo Descripción
Indica si la cuota está atrasada porque su fecha de
Indicador de atraso vencimiento ya pasó y no ha sido saldada
completamente.
Además de la tabla de amortización, esta pantalla debe incluir un botón con el texto
Volver atrás.
Al hacer clic sobre este botón, el sistema debe regresar al Home del cliente.
Listado de tarjetas de crédito
En esta sección se deben mostrar todas las tarjetas de crédito activas del cliente
autenticado.
Esta sección no debe mostrarse si el cliente no tiene tarjetas de crédito activas.
De cada tarjeta de crédito se debe mostrar la siguiente información:
Campo Descripción
Número de tarjeta Número identificador de la tarjeta. Debe mostrarse
enmascarado, dejando visibles únicamente los últimos 4
dígitos.
Límite de crédito Monto máximo aprobado para la tarjeta.
Fecha de expiración Fecha de expiración en formato MM/AA.
Monto adeudado Monto total pendiente de pago en la tarjeta.
Cada tarjeta debe tener un botón con el texto Ver detalles.
El sistema no debe mostrar el número completo de la tarjeta en el listado del Home
del cliente.
Ver detalles de una tarjeta de crédito
Al hacer clic sobre el botón Ver detalles de una tarjeta de crédito, el sistema debe
enviar al cliente a una pantalla donde se muestre el listado de consumos
registrados para esa tarjeta.
El cliente solo debe poder visualizar consumos de tarjetas que le pertenezcan.

Los consumos deben mostrarse desde el más reciente hasta el más antiguo.
Por cada consumo se debe mostrar la siguiente información:
Campo Descripción
Fecha y hora en que se registró o intentó registrar el
Fecha del consumo
consumo.
Monto consumido Monto del consumo realizado o intentado.
Nombre del comercio donde se realizó el consumo. Si
Comercio corresponde a un avance de efectivo, debe mostrarse
el texto AVANCE.
Indica si el consumo fue APROBADO o
Estado del consumo
RECHAZADO.
El estado del consumo debe mostrarse como APROBADO cuando el consumo fue
autorizado y aplicado correctamente.
El estado del consumo debe mostrarse como RECHAZADO cuando el intento de
consumo fue denegado, por ejemplo, por falta de crédito disponible, tarjeta
cancelada, tarjeta vencida o cualquier otra restricción de negocio.
Además del listado de consumos, esta pantalla debe incluir un botón con el texto
Volver atrás.
Al hacer clic sobre este botón, el sistema debe regresar al Home del cliente.
Reglas adicionales del módulo
● Solo los usuarios con rol cliente pueden acceder al Home del cliente.
● El cliente solo puede visualizar productos financieros que le pertenezcan.
● El Home debe mostrar únicamente productos financieros activos.
● Las cuentas de ahorro activas deben incluir la cuenta principal y las cuentas
secundarias.
● La cuenta principal debe mostrarse siempre en primer lugar.
● Las cuentas secundarias deben ordenarse de mayor a menor balance.
● La sección de préstamos solo debe mostrarse si el cliente tiene préstamos
activos.
● La sección de tarjetas de crédito solo debe mostrarse si el cliente tiene tarjetas de
crédito activas.

● El número completo de tarjeta de crédito no debe mostrarse en el Home ni en los
listados de detalle.
● Las transacciones de cuentas deben mostrarse desde la más reciente hasta la
más antigua.
● Los consumos de tarjetas deben mostrarse desde el más reciente hasta el más
antiguo.
● Un préstamo debe mostrarse en mora si tiene al menos una cuota vencida y no
pagada completamente.
● La opción Cerrar sesión debe cerrar la sesión activa y redirigir al Login.
Funcionalidad de Beneficiarios
Al ingresar a la opción Beneficiarios desde el menú principal del cliente, el sistema
debe enviar al usuario al módulo de administración de beneficiarios.
Este módulo permitirá al cliente registrar cuentas de ahorro de otros clientes como
beneficiarios frecuentes, con el objetivo de realizar transacciones sin tener que
ingresar manualmente el número de cuenta en cada operación.
Solo los usuarios con rol cliente podrán acceder a esta funcionalidad. Si un usuario
con rol Administrador, Cajero o Comercio intenta acceder directamente a esta
pantalla mediante la URL, el sistema debe redirigirlo a la pantalla de Acceso
denegado.
Pantalla inicial de beneficiarios
En la pantalla inicial de Beneficiarios, el sistema debe mostrar un listado con todos
los beneficiarios registrados por el cliente autenticado.
Cada beneficiario debe corresponder a una cuenta de ahorro activa existente en el
sistema.
De cada beneficiario se debe mostrar la siguiente información:
Campo Descripción
Nombre Nombre del propietario de la cuenta beneficiaria.
Apellido Apellido del propietario de la cuenta beneficiaria.
Número de cuenta Número identificador de la cuenta de ahorro
registrada como beneficiaria.
Cada beneficiario listado debe tener una acción Eliminar, que permitirá quitarlo del

listado de beneficiarios del cliente.
En la parte superior del listado debe existir un botón con el texto Agregar
beneficiario.
Agregar beneficiario
Al hacer clic sobre el botón Agregar beneficiario, el sistema debe mostrar un modal
o una pantalla con un formulario para registrar un nuevo beneficiario.
El formulario debe contener el siguiente campo:
Campo Tipo de dato Requerido Descripción
Número de cuenta Texto / string Sí Número de cuenta de ahorro que el
cliente desea registrar como
beneficiario.
Debajo del formulario deben existir dos botones:
Botón Descripción
Cancelar Cierra el modal o regresa al listado de beneficiarios sin guardar
cambios.
Guardar Valida el número de cuenta y registra al beneficiario si la
información es correcta.
Descripción del campo
Número de cuenta
Representa la cuenta de ahorro que el cliente desea registrar como beneficiario.
Este valor debe corresponder a una cuenta de ahorro activa existente en el sistema.
El número de cuenta debe contener exactamente 9 dígitos y debe almacenarse
como texto para evitar pérdida de ceros al inicio.
El cliente no debe poder registrar como beneficiario una cuenta cancelada.
El cliente tampoco debe poder registrar como beneficiario una cuenta propia, ya que
las transferencias entre cuentas propias deben realizarse desde la opción
Transferencia.

Validaciones al agregar beneficiario
El formulario para agregar beneficiario debe cumplir las siguientes validaciones:
● El número de cuenta es requerido.
● El número de cuenta debe contener exactamente 9 dígitos.
● El número de cuenta debe existir en el sistema.
● La cuenta debe estar activa.
● La cuenta no debe pertenecer al cliente autenticado.
● La cuenta no debe estar registrada previamente como beneficiario del cliente
autenticado.
Si el número de cuenta no existe, el sistema debe mostrar el siguiente mensaje:
“El número de cuenta ingresado no corresponde a una cuenta válida.”
Si la cuenta existe, pero se encuentra cancelada, el sistema debe mostrar el
siguiente mensaje:
“No puede agregar una cuenta cancelada como beneficiario.”
Si el cliente intenta registrar una cuenta propia, el sistema debe mostrar el siguiente
mensaje:
“No puede agregar una cuenta propia como beneficiario. Utilice la opción
Transferencia para mover fondos entre sus cuentas.”
Si la cuenta ya está registrada como beneficiario, el sistema debe mostrar el
siguiente mensaje:
“Esta cuenta ya se encuentra registrada como beneficiario.”
Si todas las validaciones son correctas, el sistema debe registrar el beneficiario
asociado al cliente autenticado.
El nombre y apellido del beneficiario deben obtenerse automáticamente desde el
propietario de la cuenta de ahorro registrada.
Después de registrar correctamente el beneficiario, el sistema debe mostrar el
siguiente mensaje:
“Beneficiario agregado correctamente.”
Eliminar beneficiario
Cada beneficiario del listado debe tener un botón o acción con el texto Eliminar.

Al hacer clic sobre esta acción, el sistema debe mostrar una confirmación antes de
eliminar el beneficiario.
El mensaje de confirmación debe ser el siguiente:
“¿Está seguro que desea eliminar este beneficiario?”
Debajo del mensaje deben existir dos botones:
Botón Descripción
Cancelar Cierra la confirmación sin eliminar el beneficiario.
Aceptar Elimina el beneficiario del listado del cliente.
Si el cliente cancela la acción, el sistema no debe realizar ningún cambio.
Si el cliente confirma la acción, el sistema debe eliminar la relación entre el cliente y
el beneficiario.
Eliminar un beneficiario no debe eliminar la cuenta de ahorro asociada ni afectar el
historial de transacciones realizadas previamente.
Después de eliminar correctamente el beneficiario, el sistema debe mostrar el
siguiente mensaje:
“Beneficiario eliminado correctamente.”
Reglas adicionales del módulo
● Solo los usuarios con rol Cliente pueden acceder a la funcionalidad de
Beneficiarios.
● Cada cliente solo puede visualizar y administrar sus propios beneficiarios.
● Un beneficiario debe corresponder a una cuenta de ahorro activa existente.
● No se pueden registrar cuentas canceladas como beneficiarios.
● No se pueden registrar cuentas propias como beneficiarios.
● No se puede registrar dos veces la misma cuenta como beneficiario del mismo
cliente.
● El nombre y apellido del beneficiario deben obtenerse automáticamente desde el
propietario de la cuenta.
● Eliminar un beneficiario sólo elimina la relación con el cliente autenticado.
● Eliminar un beneficiario no elimina la cuenta de ahorro ni modifica transacciones
históricas.

Funcionalidad de transacciones
El módulo de Transacciones permitirá que el cliente autenticado realice operaciones
financieras desde sus cuentas de ahorro activas. Este módulo estará compuesto por
cuatro pantallas principales: Transacción Express, Pago a tarjeta de crédito, Pago a
préstamo y Transacción a beneficiarios.
Solo los usuarios con rol cliente podrán acceder a estas funcionalidades. Si un
usuario con rol Administrador, Cajero o Comercio intenta acceder directamente a
cualquiera de estas pantallas mediante la URL, el sistema debe redirigirlo a la
pantalla de Acceso denegado.
Todas las operaciones deben validar los datos ingresados antes de afectar
balances, deudas o historiales. Cuando una transacción sea aprobada, el sistema
debe registrar los movimientos correspondientes y actualizar los balances
involucrados.
Transacción Express
La pantalla de Transacción Express permitirá que el cliente envíe dinero desde una
de sus cuentas de ahorro activas hacia otra cuenta de ahorro registrada en el
sistema, ingresando manualmente el número de cuenta destino.
El formulario debe contener los siguientes campos:
Tipo de
| Campo  |     | Requerido  |     | Descripción  |
| ------ | --- | ---------- | --- | ------------ |
dato
| Número de  | Texto /  |     | Número de cuenta de ahorro a la  |     |
| ---------- | -------- | --- | -------------------------------- | --- |
Sí
| cuenta destino  | string     |     | que se desea transferir el dinero.  |     |
| --------------- | ---------- | --- | ----------------------------------- | --- |
| Monto a         | Decimal /  |     |                                     |     |
|                 |            | Sí  | Monto que el cliente desea enviar.  |     |
| transferir      | number     |     |                                     |     |
Cuenta de ahorro activa del cliente
| Cuenta de  | Select /  |     |                                   |     |
| ---------- | --------- | --- | --------------------------------- | --- |
|            |           | Sí  | desde la cual se descontarán los  |     |
| origen     | string    |     |                                   |     |
fondos.
El selector Cuenta de origen debe mostrar únicamente las cuentas de ahorro activas
asociadas al cliente autenticado.
Validaciones de Transacción Express

El formulario de Transacción Express debe cumplir las siguientes validaciones:
● El número de cuenta destino es requerido.
● El número de cuenta destino debe existir en el sistema.
● La cuenta destino debe estar activa.
● El monto a transferir es requerido.
● El monto a transferir debe ser mayor que cero.
● La cuenta de origen es requerida.
● La cuenta de origen debe pertenecer al cliente autenticado.
● La cuenta de origen debe estar activa.
● La cuenta de origen debe tener fondos suficientes para cubrir el monto indicado.
● La cuenta de origen y la cuenta destino no pueden ser la misma cuenta.
Si la cuenta destino no existe o no está activa, el sistema debe mostrar el siguiente
mensaje:
“El número de cuenta ingresado no corresponde a una cuenta válida.”
Si la cuenta de origen no tiene fondos suficientes, el sistema debe mostrar el
siguiente mensaje:
“El monto ingresado excede el saldo disponible de la cuenta seleccionada.”
Si el cliente intenta transferir a la misma cuenta seleccionada como origen, el
sistema debe mostrar el siguiente mensaje:
“La cuenta destino no puede ser la misma cuenta de origen.”
Confirmación de Transacción Express
Si todas las validaciones son correctas, el sistema debe enviar al cliente a una
pantalla de confirmación antes de ejecutar la transacción.
En esta pantalla se debe mostrar el nombre y apellido del titular de la cuenta
destino, el número de cuenta destino y el monto a transferir.
La pantalla debe mostrar el siguiente mensaje:
“¿Está seguro de que desea realizar esta transacción?”
Debajo del mensaje deben existir dos botones:

Botón Descripción
Cancela la operación y redirige al cliente al Home del
Cancelar
cliente.
Confirmar Ejecuta la transferencia.
Si el cliente cancela la operación, la transacción no debe ejecutarse y el sistema
debe redirigirlo al Home del cliente.
Si el cliente confirma la operación, el sistema debe descontar el monto de la cuenta
de origen y acreditar el mismo monto en la cuenta destino.
Registro de Transacción Express
Cuando la Transacción Express sea aprobada, el sistema debe registrar dos
movimientos:
● Una transacción de tipo DÉBITO en la cuenta de origen.
● Una transacción de tipo CRÉDITO en la cuenta destino.
La transacción registrada en la cuenta de origen debe mostrar como beneficiario el
número de cuenta destino.
La transacción registrada en la cuenta destino debe mostrar como origen el número
de cuenta desde la cual se enviaron los fondos.
Ambas transacciones deben quedar en estado APROBADA.
Si la transacción es rechazada por fondos insuficientes, el sistema debe registrar el
intento como RECHAZADO en la cuenta de origen, sin afectar ningún balance.
Correos de Transacción Express
Después de confirmar y procesar correctamente la transacción, el sistema debe
enviar dos correos electrónicos.
El primer correo debe enviarse al cliente que realizó la transacción.
Asunto sugerido:
“Transacción realizada a la cuenta [XXXX]”
Donde [XXXX] corresponde a los últimos cuatro dígitos del número de cuenta
destino.

El cuerpo del correo debe incluir:
● Monto transferido.
● Fecha de la transacción.
● Hora exacta de la transacción.
● Últimos cuatro dígitos de la cuenta destino.
El segundo correo debe enviarse al cliente receptor de los fondos.
Asunto sugerido:
“Transacción enviada desde la cuenta [XXXX]”
Donde [XXXX] corresponde a los últimos cuatro dígitos de la cuenta origen.
El cuerpo del correo debe incluir:
● Monto recibido.
● Fecha de la transacción.
● Hora exacta de la transacción.
● Últimos cuatro dígitos de la cuenta origen.
Si ocurre un error al enviar alguno de los correos, la transacción no debe revertirse.
El sistema debe registrar el error y mostrar un mensaje informativo.
Mensaje sugerido:
“La transacción fue realizada correctamente, pero no fue posible enviar una o más
notificaciones por correo.”
Al finalizar el proceso, el sistema debe redirigir al cliente al Home del cliente.
Pago a tarjeta de crédito
La pantalla de Pago a tarjeta de crédito permitirá que el cliente realice pagos a sus
tarjetas de crédito activas utilizando fondos disponibles en una de sus cuentas de
ahorro activas.
El formulario debe contener los siguientes campos:
Campo Tipo de dato Requerido Descripción
Tarjeta de crédito Tarjeta de crédito activa del cliente a la que
Select / string Sí
destino se aplicará el pago.

Campo Tipo de dato Requerido Descripción
Cuenta de ahorro activa del cliente desde la
Cuenta de origen Select / string Sí
cual se descontará el dinero.
Decimal / Monto que el cliente desea pagar a la
Monto a pagar Sí
number tarjeta.
El selector Tarjeta de crédito destino debe mostrar únicamente las tarjetas de
crédito activas asociadas al cliente autenticado.
El selector Cuenta de origen debe mostrar únicamente las cuentas de ahorro activas
asociadas al cliente autenticado.
Validaciones de pago a tarjeta de crédito
El formulario de pago a tarjeta de crédito debe cumplir las siguientes validaciones:
● La tarjeta de crédito destino es requerida.
● La tarjeta seleccionada debe pertenecer al cliente autenticado.
● La tarjeta seleccionada debe estar activa.
● La cuenta de origen es requerida.
● La cuenta de origen debe pertenecer al cliente autenticado.
● La cuenta de origen debe estar activa.
● El monto a pagar es requerido.
● El monto a pagar debe ser mayor que cero.
● La cuenta de origen debe tener fondos suficientes para cubrir el monto efectivo
que será aplicado al pago.
● La tarjeta debe tener deuda pendiente.
Si la cuenta de origen no tiene fondos suficientes, el sistema debe mostrar el
siguiente mensaje:
“No dispone del monto requerido en la cuenta seleccionada.”
Si la tarjeta no tiene deuda pendiente, el sistema debe mostrar el siguiente mensaje:
“La tarjeta seleccionada no tiene deuda pendiente.”
Regla para evitar sobrepago de tarjeta
Si el monto ingresado por el cliente es mayor que la deuda actual de la tarjeta, el
sistema no debe descontar el monto completo ingresado.

En ese caso, el sistema debe tomar como monto efectivo de pago únicamente el
valor correspondiente a la deuda actual de la tarjeta.
Ejemplo:
Si la tarjeta tiene una deuda de RD$500.00 y el cliente intenta pagar RD$1,000.00,
el sistema solo debe debitar RD$500.00 de la cuenta de origen y aplicar
RD$500.00 a la tarjeta.
El excedente no debe descontarse ni utilizarse.
Procesamiento del pago a tarjeta de crédito
Cuando el pago sea aprobado, el sistema debe realizar las siguientes acciones:
● Debitar el monto efectivo de pago desde la cuenta de ahorro origen.
● Reducir la deuda de la tarjeta de crédito por el monto efectivo pagado.
● Actualizar el crédito disponible de la tarjeta.
● Registrar la transacción en el historial de la cuenta de ahorro origen como
DÉBITO.
● Registrar la operación como pago aplicado a la tarjeta.
La transacción en la cuenta de ahorro debe mostrar como beneficiario los últimos
cuatro dígitos de la tarjeta pagada.
El origen de la transacción debe ser el número de cuenta desde la cual se realizó el
pago.
La transacción debe quedar en estado APROBADA.
Si el pago es rechazado por fondos insuficientes, el sistema debe registrar el intento
como RECHAZADO sin afectar el balance de la cuenta ni la deuda de la tarjeta.
Correo de pago a tarjeta de crédito
Una vez procesado correctamente el pago, el sistema debe enviar un correo
electrónico al cliente notificando la operación.
Asunto sugerido:
“Pago realizado a la tarjeta [XXXX]”
Donde [XXXX] corresponde a los últimos cuatro dígitos de la tarjeta pagada.
El cuerpo del correo debe incluir:

● Monto pagado.
● Últimos cuatro dígitos de la cuenta desde la cual se realizó el pago.
● Últimos cuatro dígitos de la tarjeta pagada.
● Fecha de la transacción.
● Hora exacta de la transacción.
Si ocurre un error al enviar el correo, el pago no debe revertirse. El sistema debe
registrar el error y mostrar un mensaje informativo.
Al finalizar el proceso, el sistema debe redirigir al cliente al Home del cliente.
Pago a préstamo
La pantalla de Pago a préstamo permitirá que el cliente realice abonos a sus
préstamos activos utilizando fondos disponibles en una de sus cuentas de ahorro
activas.
El formulario debe contener los siguientes campos:
Campo Tipo de dato Requerido Descripción
Préstamo activo del cliente al que se
Préstamo a pagar Select / string Sí
aplicará el pago.
Cuenta de ahorro activa del cliente
Cuenta de origen Select / string Sí
desde la cual se descontará el dinero.
Decimal / Monto que el cliente desea abonar al
Monto a pagar Sí
number préstamo.
El selector Préstamo a pagar debe mostrar únicamente los préstamos activos del
cliente autenticado.
El selector Cuenta de origen debe mostrar únicamente las cuentas de ahorro activas
asociadas al cliente autenticado.
Validaciones de pago a préstamo
El formulario de pago a préstamo debe cumplir las siguientes validaciones:
● El préstamo a pagar es requerido.
● El préstamo seleccionado debe pertenecer al cliente autenticado.
● El préstamo seleccionado debe estar activo.
● La cuenta de origen es requerida.

● La cuenta de origen debe pertenecer al cliente autenticado.
● La cuenta de origen debe estar activa.
● El monto a pagar es requerido.
● El monto a pagar debe ser mayor que cero.
● La cuenta de origen debe tener fondos suficientes para cubrir el monto efectivo
que será aplicado al préstamo.
● El préstamo debe tener cuotas pendientes.
Si la cuenta de origen no tiene fondos suficientes, el sistema debe mostrar el
siguiente mensaje:
“No dispone del monto requerido en la cuenta seleccionada.”
Si el préstamo no tiene cuotas pendientes, el sistema debe mostrar el siguiente
mensaje:
“El préstamo seleccionado no tiene cuotas pendientes de pago.”
Regla para evitar pagos superiores a la deuda del préstamo
Si el monto ingresado por el cliente es mayor que el monto total pendiente del
préstamo, el sistema no debe descontar el monto completo ingresado.
En ese caso, el sistema debe tomar como monto efectivo de pago únicamente el
valor pendiente real del préstamo.
Ejemplo:
Si el préstamo tiene un balance pendiente de RD$2,000.00 y el cliente intenta
pagar RD$3,000.00, el sistema solo debe debitar RD$2,000.00 de la cuenta de
origen y aplicar RD$2,000.00 al préstamo.
El excedente no debe descontarse de la cuenta del cliente.
Aplicación del pago al préstamo
Una vez validado el formulario, el sistema debe aplicar el pago siguiendo el orden
de la tabla de amortización.
El pago debe aplicarse primero a la cuota pendiente más antigua, es decir, la cuota
más cercana en fecha que no haya sido pagada completamente.
El monto pagado puede aplicarse de las siguientes formas:
● Si el monto alcanza para completar la cuota pendiente, la cuota debe marcarse
como pagada.

● Si el monto no alcanza para completar la cuota pendiente, la cuota debe quedar
parcialmente pagada.
● Si después de pagar una cuota queda saldo disponible del monto efectivo
pagado, el sistema debe continuar aplicando el excedente a la siguiente cuota
pendiente.
● Este proceso debe repetirse hasta agotar el monto efectivo pagado o hasta que
no existan más cuotas pendientes.
Si todas las cuotas del préstamo quedan pagadas, el préstamo debe actualizarse al
estado completo.
Si una cuota atrasada es pagada completamente, el indicador de atraso debe
actualizarse para que la cuota no siga marcada como atrasada.
Procesamiento del pago a préstamo
Cuando el pago sea aprobado, el sistema debe realizar las siguientes acciones:
● Debitar el monto efectivo de pago desde la cuenta de ahorro origen.
● Aplicar el monto del préstamo siguiendo la tabla de amortización.
● Actualizar el estado de las cuotas afectadas.
● Actualizar el monto pendiente del préstamo.
● Registrar la transacción en el historial de la cuenta de ahorro origen como
DÉBITO.
● Registrar la operación como pago aplicado al préstamo.
La transacción en la cuenta de ahorro debe mostrar como beneficiario el número
identificador del préstamo.
El origen de la transacción debe ser el número de cuenta desde la cual se realizó el
pago.
La transacción debe quedar en estado APROBADA.
Si el pago es rechazado por fondos insuficientes, el sistema debe registrar el intento
como RECHAZADO sin afectar el balance de la cuenta ni las cuotas del préstamo.
Correo de pago a préstamo
Una vez procesado correctamente el pago, el sistema debe enviar un correo
electrónico al cliente notificando la operación.
Asunto sugerido:
“Pago realizado al préstamo [XXXXXXXXX]”

Donde [XXXXXXXXX] corresponde al número identificador de 9 dígitos del
préstamo.
El cuerpo del correo debe incluir:
● Monto pagado.
● Número del préstamo.
● Últimos cuatro dígitos de la cuenta desde la cual se realizó el pago.
● Fecha de la transacción.
● Hora exacta de la transacción.
Si ocurre un error al enviar el correo, el pago no debe revertirse. El sistema debe
registrar el error y mostrar un mensaje informativo.
Al finalizar el proceso, el sistema debe redirigir al cliente al Home del cliente.
Transacción a beneficiarios
La pantalla de Transacción a beneficiarios permitirá que el cliente transfiere fondos
desde una de sus cuentas de ahorro activas hacia una cuenta previamente
registrada como beneficiario.
El formulario debe contener los siguientes campos:
Campo Tipo de dato Requerido Descripción
Beneficiario previamente
Beneficiario Select / string Sí registrado por el cliente
autenticado.
Monto a Decimal / Monto que el cliente desea
Sí
transferir number transferir al beneficiario.
Cuenta de ahorro activa del cliente
Cuenta de
Select / string Sí desde la cual se descontarán los
origen
fondos.
El selector Beneficiario debe mostrar el número de cuenta y el nombre del
beneficiario.
El selector Cuenta de origen debe mostrar únicamente las cuentas de ahorro activas
asociadas al cliente autenticado.
Validaciones de transacción a beneficiarios

El formulario de transacción a beneficiarios debe cumplir las siguientes
validaciones:
● El beneficiario es requerido.
● El beneficiario debe pertenecer al cliente autenticado.
● La cuenta del beneficiario debe existir.
● La cuenta del beneficiario debe estar activa.
● El monto a transferir es requerido.
● El monto a transferir debe ser mayor que cero.
● La cuenta de origen es requerida.
● La cuenta de origen debe pertenecer al cliente autenticado.
● La cuenta de origen debe estar activa.
● La cuenta de origen debe tener fondos suficientes para cubrir el monto indicado.
Si el cliente no tiene beneficiarios registrados, el sistema debe mostrar el siguiente
mensaje:
“No tiene beneficiarios registrados.”
Si la cuenta del beneficiario no existe o está cancelada, el sistema debe mostrar el
siguiente mensaje:
“La cuenta del beneficiario no se encuentra disponible.”
Si la cuenta de origen no tiene fondos suficientes, el sistema debe mostrar el
siguiente mensaje:
“No dispone de fondos suficientes para realizar esta transacción.”
Confirmación de transacción a beneficiarios
Si todas las validaciones son correctas, el sistema debe enviar al cliente a una
pantalla de confirmación antes de ejecutar la transacción.
En esta pantalla se debe mostrar el nombre y apellido del titular de la cuenta
beneficiaria, el número de cuenta beneficiaria y el monto a transferir.
La pantalla debe mostrar el siguiente mensaje:
“¿Está seguro de que desea realizar esta transacción?”
Debajo del mensaje deben existir dos botones:

Botón Descripción
Cancela la operación y redirige al cliente al Home del
Cancelar
cliente.
Confirmar Ejecuta la transferencia al beneficiario.
Si el cliente cancela la operación, la transacción no debe ejecutarse y el sistema
debe redirigirlo al Home del cliente.
Si el cliente confirma la operación, el sistema debe descontar el monto de la cuenta
de origen seleccionada y acreditar el mismo monto en la cuenta del beneficiario.
Registro de transacción a beneficiarios
Cuando la transacción a beneficiarios sea aprobada, el sistema debe registrar dos
movimientos:
● Una transacción de tipo DÉBITO en la cuenta de origen.
● Una transacción de tipo CRÉDITO en la cuenta del beneficiario.
La transacción registrada en la cuenta de origen debe mostrar como beneficiario el
número de cuenta del beneficiario.
La transacción registrada en la cuenta del beneficiario debe mostrar como origen el
número de cuenta desde la cual se enviaron los fondos.
Ambas transacciones deben quedar en estado APROBADA.
Si la transacción es rechazada por fondos insuficientes, el sistema debe registrar el
intento como RECHAZADO en la cuenta de origen, sin afectar ningún balance.
Correos de transacción a beneficiarios
Después de confirmar y procesar correctamente la transacción, el sistema debe
enviar dos correos electrónicos.
El primer correo debe enviarse al cliente que realizó la transacción.
Asunto sugerido:
“Transacción realizada a la cuenta [XXXX]”
Donde [XXXX] corresponde a los últimos cuatro dígitos de la cuenta del beneficiario.

El cuerpo del correo debe incluir:
● Monto transferido.
● Fecha de la transacción.
● Hora exacta de la transacción.
● Últimos cuatro dígitos de la cuenta destino.
El segundo correo debe enviarse al cliente receptor de los fondos.
Asunto sugerido:
“Transacción enviada desde la cuenta [XXXX]”
Donde [XXXX] corresponde a los últimos cuatro dígitos de la cuenta origen.
El cuerpo del correo debe incluir:
● Monto recibido.
● Fecha de la transacción.
● Hora exacta de la transacción.
● Últimos cuatro dígitos de la cuenta origen.
Si ocurre un error al enviar alguno de los correos, la transacción no debe revertirse.
El sistema debe registrar el error y mostrar un mensaje informativo.
Al finalizar el proceso, el sistema debe redirigir al cliente al Home del cliente.
Reglas adicionales del módulo
● Solo los usuarios con rol cliente pueden acceder a las pantallas de transacciones.
● El cliente solo puede utilizar cuentas de ahorro activas que le pertenezcan.
● Todas las transacciones deben validar fondos suficientes antes de afectar los
balances.
● Todo monto ingresado debe ser mayor que cero.
● Las transacciones aprobadas deben actualizar los balances correspondientes.
● Las transacciones aprobadas deben registrarse en el historial de las cuentas
involucradas.
● Las salidas de dinero desde una cuenta de ahorro deben registrarse como
DÉBITO.
● Los ingresos de dinero a una cuenta de ahorro deben registrarse como CRÉDITO.
● Los pagos a tarjetas de crédito y préstamos deben registrarse como DÉBITO en la
cuenta de ahorro origen.
● Los pagos a tarjetas de crédito no pueden exceder la deuda actual de la tarjeta.
● Los pagos a préstamos no pueden exceder el monto pendiente real del préstamo.
● El sistema no debe descontar excedentes que no puedan aplicarse a una tarjeta o

préstamo.
● Las transacciones rechazadas no deben modificar balances ni deudas.
● Las operaciones confirmadas no deben revertirse por errores en el envío de
correos.
● Al finalizar cualquier operación, el sistema debe redirigir al cliente al Home del
cliente.
Funcionalidad de Avances de efectivo
Al ingresar a la opción Avance de efectivo desde el menú principal del cliente, el
sistema debe enviar al usuario a la pantalla de avance de efectivo.
Esta funcionalidad permitirá que el cliente transfiere fondos desde una de sus
tarjetas de crédito activas hacia una de sus cuentas de ahorro activas. El monto
recibido será depositado en la cuenta de ahorro seleccionada y, al mismo tiempo, se
generará una deuda en la tarjeta de crédito utilizada.
Solo los usuarios con rol cliente podrán acceder a esta funcionalidad. Si un usuario
con rol Administrador, Cajero o Comercio intenta acceder directamente a esta
pantalla mediante la URL, el sistema debe redirigirlo a la pantalla de Acceso
denegado.
Formulario de avance de efectivo
La pantalla debe mostrar un formulario para que el cliente pueda seleccionar la
tarjeta de crédito origen, la cuenta de ahorro destino y el monto que desea solicitar
como avance.
El formulario debe contener los siguientes campos:
Campo Tipo de dato Requerido Descripción
Tarjeta de crédito Select / string Sí Tarjeta de crédito activa del
origen cliente desde la cual se
realizará el avance.
Cuenta de ahorro Select / string Sí Cuenta de ahorro activa del
destino cliente donde será
depositado el dinero.
Monto del avance Decimal / Sí Monto que el cliente desea
de efectivo number recibir en su cuenta de
ahorro.

El selector Tarjeta de crédito origen debe mostrar únicamente las tarjetas de crédito
activas asociadas al cliente autenticado.
El selector Cuenta de ahorro destino debe mostrar únicamente las cuentas de
ahorro activas asociadas al cliente autenticado.
Debajo del formulario debe existir un botón con el texto Realizar avance.
Descripción de campos
Tarjeta de crédito origen
Representa la tarjeta de crédito desde la cual se tomará el avance de efectivo.
La tarjeta debe pertenecer al cliente autenticado, debe estar activa y no debe estar
vencida.
Cuenta de ahorro destino
Representa la cuenta de ahorro donde el cliente recibirá el monto del avance.
La cuenta debe pertenecer al cliente autenticado y debe estar activa.
Monto del avance de efectivo
Representa el monto que será depositado en la cuenta de ahorro destino.
Este monto no incluye el interés aplicado por la operación.
El interés del avance será calculado automáticamente por el sistema y se sumará a
la deuda de la tarjeta de crédito.
Validaciones del avance de efectivo
El formulario de avance de efectivo debe cumplir las siguientes validaciones:
● La tarjeta de crédito origen es requerida.
● La tarjeta seleccionada debe pertenecer al cliente autenticado.
● La tarjeta seleccionada debe estar activa.
● La tarjeta seleccionada no debe estar vencida.
● La cuenta de ahorro destino es requerida.
● La cuenta de ahorro destino debe pertenecer al cliente autenticado.
● La cuenta de ahorro destino debe estar activa.
● El monto del avance es requerido.
● El monto del avance debe ser mayor que cero.
● La tarjeta debe tener crédito disponible suficiente para cubrir el monto del avance

más el interés generado.
Si la tarjeta seleccionada no está activa, el sistema debe mostrar el siguiente
mensaje:
“La tarjeta seleccionada no se encuentra activa.”
Si la tarjeta seleccionada está vencida, el sistema debe mostrar el siguiente
mensaje:
“La tarjeta seleccionada se encuentra vencida.”
Si la cuenta de ahorro destino no está activa, el sistema debe mostrar el siguiente
mensaje:
“La cuenta de ahorro seleccionada no se encuentra activa.”
Si el monto del avance es menor o igual a cero, el sistema debe mostrar el siguiente
mensaje:
“El monto del avance debe ser mayor que cero.”
Validación del crédito disponible
Antes de aprobar el avance, el sistema debe calcular el crédito disponible de la
tarjeta.
El crédito disponible debe calcularse de la siguiente manera:
Crédito disponible = Límite de crédito de la tarjeta - Deuda actual de la tarjeta
El sistema también debe calcular el interés del avance:
Interés del avance = Monto del avance x 6.25%
El total que será cargado como deuda a la tarjeta debe calcularse de la siguiente
manera:
Total a cargar a la tarjeta = Monto del avance + Interés del avance
Para aprobar la operación, el total a cargar a la tarjeta no debe superar el crédito
disponible.
Ejemplo:
Si una tarjeta tiene un límite de RD$500.00 y una deuda actual de RD$300.00, el
crédito disponible será RD$200.00.

Si el cliente intenta realizar un avance de RD$200.00, el sistema debe calcular el
interés de 6.25%, equivalente a RD$12.50.
El total a cargar sería RD$212.50, por lo tanto, la operación debe ser rechazada
porque supera el crédito disponible.
Esta validación evita que la deuda total de la tarjeta exceda el límite de crédito
aprobado.
Si el total a cargar supera el crédito disponible, el sistema debe mostrar el siguiente
mensaje:
“El avance solicitado excede el crédito disponible de la tarjeta seleccionada.”
Procesamiento del avance de efectivo
Si todas las validaciones son correctas, el sistema debe procesar el avance de
efectivo.
El sistema debe realizar las siguientes acciones:
● Acreditar el monto del avance en la cuenta de ahorro destino.
● Aumentar la deuda de la tarjeta de crédito por el monto del avance más el interés
generado.
● Registrar una transacción de tipo CRÉDITO en la cuenta de ahorro destino.
● Registrar un consumo en la tarjeta de crédito con el comercio AVANCE.
● Actualizar el crédito disponible de la tarjeta.
El monto acreditado en la cuenta de ahorro debe ser únicamente el monto del
avance solicitado.
La deuda agregada a la tarjeta debe ser el monto del avance más el interés del
6.25%.
Ejemplo:
Si el cliente realiza un avance de RD$100.00, el sistema debe calcular un interés de
RD$6.25.
La cuenta de ahorro destino debe recibir RD$100.00.
La tarjeta de crédito debe aumentar su deuda en RD$106.25.
Registro de la transacción
En la cuenta de ahorro destino se debe registrar una transacción de tipo CRÉDITO.

Esta transacción debe reflejar que el dinero fue recibido desde una tarjeta de crédito
mediante un avance de efectivo.
La transacción debe mostrar como origen los últimos cuatro dígitos de la tarjeta
utilizada.
La transacción debe quedar en estado APROBADA.
En la tarjeta de crédito se debe registrar un consumo con las siguientes
características:
Campo Descripción
Comercio Debe mostrarse el texto AVANCE.
Monto del consumo Debe corresponder al monto total cargado a la tarjeta:
monto del avance más interés.
Estado Debe registrarse como APROBADO si la operación fue
realizada correctamente.
Fecha Fecha y hora exacta en que se realizó el avance.
Si la operación es rechazada por falta de crédito disponible, el sistema debe
registrar el intento como RECHAZADO en el historial de consumos de la tarjeta, sin
afectar la cuenta de ahorro ni la deuda de la tarjeta.
Correo de avance de efectivo
Una vez procesado correctamente el avance de efectivo, el sistema debe enviar un
correo electrónico al cliente notificando la operación.
El asunto del correo debe ser:
“Avance de efectivo desde la tarjeta [XXXX]”
Donde [XXXX] corresponde a los últimos cuatro dígitos del número de la tarjeta
utilizada.
El cuerpo del correo debe incluir:
● Monto del avance realizado.
● Interés aplicado por el avance.
● Total cargado a la tarjeta.
● Últimos cuatro dígitos de la cuenta de ahorro donde fue depositado el dinero.
● Fecha de la transacción.

● Hora exacta de la transacción.
El correo puede tener un contenido como el siguiente:
Asunto: Avance de efectivo desde la tarjeta [XXXX]
Hola [Nombre del cliente],
Se ha realizado un avance de efectivo desde su tarjeta terminada en [XXXX].
Monto depositado: RD$[Monto del avance]
Interés aplicado: RD$[Interés]
Total cargado a la tarjeta: RD$[Total cargado]
Cuenta destino terminada en: [Últimos 4 dígitos de la cuenta]
Fecha y hora: [Fecha y hora]
Si usted no reconoce esta operación, comuníquese con la entidad bancaria.
Si ocurre un error al enviar el correo, la operación no debe revertirse. El sistema
debe registrar el error y mostrar un mensaje informativo.
Mensaje sugerido:
“El avance fue realizado correctamente, pero no fue posible enviar el correo de
notificación.”
Finalmente, una vez completada la operación, el sistema debe redirigir al cliente a
su pantalla principal, es decir, al Home del cliente.
Reglas adicionales del módulo
● Solo los usuarios con rol Cliente pueden acceder a la funcionalidad de Avance de
efectivo.
● El cliente solo puede utilizar tarjetas de crédito activas que le pertenezcan.
● El cliente solo puede seleccionar cuentas de ahorro activas que le pertenezcan.
● El monto del avance debe ser mayor que cero.
● La operación debe validar el crédito disponible antes de afectar balances o
deudas.
● El crédito disponible debe considerar la deuda actual de la tarjeta.
● El total cargado a la tarjeta debe incluir el monto del avance más el interés del
6.25%.
● El total cargado a la tarjeta no puede superar el crédito disponible.
● El monto depositado en la cuenta de ahorro debe ser únicamente el monto del
avance solicitado.
● La cuenta de ahorro destino debe registrar la operación como CRÉDITO.

● La tarjeta de crédito debe registrar la operación como consumo de tipo AVANCE.
● Los avances rechazados no deben modificar el balance de la cuenta ni la deuda
de la tarjeta.
● Los avances aprobados deben actualizar el balance de la cuenta, la deuda de la
tarjeta y el crédito disponible.
● La operación no debe revertirse si falla el envío del correo electrónico.
● Al finalizar la operación, el sistema debe redirigir al cliente al Home del cliente.
Funcionalidad de Transferencia entre cuentas
Al ingresar a la opción Transferencia desde el menú principal del cliente, el sistema
debe enviar al usuario a la pantalla de transferencia entre cuentas propias.
Esta funcionalidad permitirá que el cliente autenticado mueva fondos entre sus
propias cuentas de ahorro activas, seleccionando una cuenta de origen, una cuenta
de destino y el monto que desea transferir.
Solo los usuarios con rol cliente podrán acceder a esta funcionalidad. Si un usuario
con rol Administrador, Cajero o Comercio intenta acceder directamente a esta
pantalla mediante la URL, el sistema debe redirigirlo a la pantalla de Acceso
denegado.
Formulario de transferencia entre cuentas
La pantalla debe mostrar un formulario para que el cliente pueda seleccionar la
cuenta desde la cual desea descontar los fondos y la cuenta donde desea recibirlos.
El formulario debe contener los siguientes campos:
Campo Tipo de dato Requerido Descripción
Cuenta de ahorro activa del cliente
Cuenta de origen Select / string Sí
desde la cual se descontará el dinero.
Cuenta de ahorro activa del mismo
Cuenta de destino Select / string Sí
cliente donde se acreditará el dinero.
Decimal / Cantidad de dinero que el cliente
Monto a transferir Sí
number desea transferir entre sus cuentas.
Los selectores de cuenta de origen y cuenta de destino deben mostrar únicamente
cuentas de ahorro activas asociadas al cliente autenticado.

Debajo del formulario debe existir un botón con el texto Realizar transferencia.
Descripción de campos
Cuenta de origen
Representa la cuenta de ahorro desde la cual se descontará el monto de la
transferencia.
La cuenta debe pertenecer al cliente autenticado y debe estar activa.
Cuenta de destino
Representa la cuenta de ahorro donde se acreditará el monto transferido.
La cuenta debe pertenecer al mismo cliente autenticado y debe estar activa.
Monto a transferir
Representa la cantidad de dinero que será movida desde la cuenta de origen hacia
la cuenta de destino.
Este monto debe ser mayor que cero y no puede exceder el balance disponible de la
cuenta de origen.
Validaciones de transferencia entre cuentas
El formulario de transferencia entre cuentas debe cumplir las siguientes
validaciones:
● La cuenta de origen es requerida.
● La cuenta de destino es requerida.
● El monto a transferir es requerido.
● El monto a transferir debe ser mayor que cero.
● La cuenta de origen debe pertenecer al cliente autenticado.
● La cuenta de destino debe pertenecer al cliente autenticado.
● La cuenta de origen debe estar activa.
● La cuenta de destino debe estar activa.
● La cuenta de origen y la cuenta de destino no pueden ser la misma cuenta.
● La cuenta de origen debe tener fondos suficientes para cubrir el monto indicado.
● El cliente debe tener al menos dos cuentas de ahorro activas para poder realizar
una transferencia entre cuentas propias.
Si el cliente no tiene al menos dos cuentas de ahorro activas, el sistema debe
mostrar el siguiente mensaje:

“Debe tener al menos dos cuentas de ahorro activas para realizar una transferencia
entre cuentas.”
Si la cuenta de origen y la cuenta de destino son la misma, el sistema debe mostrar
el siguiente mensaje:
“La cuenta de origen y la cuenta de destino no pueden ser la misma.”
Si el monto ingresado es menor o igual a cero, el sistema debe mostrar el siguiente
mensaje:
“El monto a transferir debe ser mayor que cero.”
Si la cuenta de origen no tiene fondos suficientes, el sistema debe mostrar el
siguiente mensaje:
“No dispone del monto requerido en la cuenta seleccionada.”
Confirmación de transferencia
Si todas las validaciones son correctas, el sistema debe enviar al cliente a una
pantalla de confirmación antes de ejecutar la transferencia.
En esta pantalla se debe mostrar la siguiente información:
Campo Descripción
Número de la cuenta desde la cual se descontarán
Cuenta de origen
los fondos.
Número de la cuenta donde se acreditarán los
Cuenta de destino
fondos.
Monto a transferir Monto que será movido entre las cuentas.
La pantalla debe mostrar el siguiente mensaje:
“¿Está seguro que desea realizar esta transferencia?”
Debajo del mensaje deben existir dos botones:

Botón Descripción
Cancela la operación y redirige al cliente al Home del
Cancelar
cliente.
Ejecuta la transferencia entre las cuentas
Confirmar
seleccionadas.
Si el cliente cancela la operación, la transferencia no debe ejecutarse y el sistema
debe redirigirlo al Home del cliente.
Procesamiento de la transferencia
Si el cliente confirma la operación, el sistema debe procesar la transferencia de
fondos.
El sistema debe realizar las siguientes acciones:
● Descontar el monto indicado de la cuenta de origen.
● Acreditar el mismo monto en la cuenta de destino.
● Registrar una transacción de tipo DÉBITO en la cuenta de origen.
● Registrar una transacción de tipo CRÉDITO en la cuenta de destino.
● Actualizar el balance de ambas cuentas.
La transferencia debe realizarse de forma transaccional. Esto significa que, si ocurre
un error al actualizar una de las cuentas, el sistema no debe aplicar parcialmente la
operación.
Registro de la transferencia
En la cuenta de origen, la operación debe registrarse como una transacción de tipo
DÉBITO, ya que representa una salida de fondos.
En la cuenta de destino, la operación debe registrarse como una transacción de tipo
CRÉDITO, ya que representa una entrada de dinero.
La transacción registrada en la cuenta de origen debe mostrar como beneficiario el
número de cuenta destino.
La transacción registrada en la cuenta de destino debe mostrar como origen el
número de cuenta origen.
Ambas transacciones deben quedar en estado APROBADA.

Si la operación es rechazada por fondos insuficientes o por alguna validación de
negocio, el sistema debe registrar el intento como RECHAZADO en la cuenta de
origen, sin afectar ningún balance.
Correo de transferencia entre cuentas
Una vez procesada correctamente la transferencia, el sistema debe enviar un correo
electrónico al cliente notificando la operación.
El asunto del correo debe ser:
“Transferencia entre cuentas realizada”
El cuerpo del correo debe incluir:
● Monto transferido.
● Últimos cuatro dígitos de la cuenta de origen.
● Últimos cuatro dígitos de la cuenta de destino.
● Fecha de la transferencia.
● Hora exacta de la transferencia.
El correo puede tener un contenido como el siguiente:
Asunto: Transferencia entre cuentas realizada
Hola [Nombre del cliente],
Se ha realizado una transferencia entre sus cuentas de ahorro.
Cuenta origen terminada en: [XXXX]
Cuenta destino terminada en: [XXXX]
Monto transferido: RD$[Monto]
Fecha y hora: [Fecha y hora]
Si usted no reconoce esta operación, comuníquese con la entidad bancaria.
Si ocurre un error al enviar el correo, la transferencia no debe revertirse. El sistema
debe registrar el error y mostrar un mensaje informativo.
Mensaje sugerido:
“La transferencia fue realizada correctamente, pero no fue posible enviar el correo
de notificación.”
Finalmente, una vez completada la operación, el sistema debe redirigir al cliente a
su pantalla principal, es decir, al Home del cliente.

Reglas adicionales del módulo
● Solo los usuarios con rol Cliente pueden acceder a la funcionalidad de
Transferencia entre cuentas.
● El cliente solo puede transferir fondos entre cuentas de ahorro que le
pertenezcan.
● Sólo deben mostrarse cuentas de ahorro activas en los selectores.
● La cuenta de origen y la cuenta de destino no pueden ser la misma cuenta.
● El monto a transferir debe ser mayor que cero.
● La cuenta de origen debe tener fondos suficientes antes de aprobar la
transferencia.
● La cuenta de origen debe registrar la operación como DÉBITO.
● La cuenta de destino debe registrar la operación como CRÉDITO.
● Las transferencias aprobadas deben actualizar los balances de ambas cuentas.
● Las transferencias rechazadas no deben modificar balances.
● La operación debe ejecutarse de forma transaccional para evitar inconsistencias.
● Al finalizar la operación, el sistema debe redirigir al cliente al Home del cliente.
Funcionalidades Cajero
Home
Luego de iniciar sesión correctamente con un usuario de tipo Cajero, el sistema debe
redirigir automáticamente al Home del cajero.
Esta pantalla funcionará como el panel principal del cajero dentro de la aplicación
web. Desde ella, el cajero podrá acceder a las operaciones que tiene permitidas y
visualizar indicadores relacionados únicamente con las transacciones realizadas por
él durante el día actual.
Solo los usuarios con rol Cajero podrán acceder a esta pantalla. Si un usuario con rol
Administrador, Cliente o Comercio intenta acceder directamente mediante la URL, el
sistema debe redirigirlo a la pantalla de Acceso denegado.
Menú principal del cajero
En el Home del cajero, el sistema debe mostrar un menú de navegación con las
opciones disponibles para este rol.
El menú debe contener las siguientes opciones:

Opción del menú Descripción
Home Envía al panel principal del cajero.
Depósito Permite realizar depósitos a cuentas de ahorro.
Retiro Permite realizar retiros desde cuentas de ahorro.
Pago a tarjeta de crédito Permite registrar pagos a tarjetas de crédito.
Pago a préstamo Permite registrar pagos a préstamos.
Transacciones a cuentas de Permite realizar transferencias a cuentas de ahorro
terceros de terceros.
Cierra la sesión activa del cajero y lo redirige a la
Cerrar sesión
pantalla de Login.
La opción Cerrar sesión debe eliminar la sesión activa del usuario y redirigirlo a la
pantalla de Login.
Indicadores del Home del cajero
Además del menú principal, el Home del cajero debe mostrar indicadores
operativos correspondientes al cajero autenticado.
Estos indicadores deben calcularse únicamente con las operaciones realizadas por
el cajero que se encuentra logueado y solo deben tomar en cuenta la fecha actual
del sistema.
El sistema debe mostrar los siguientes indicadores:
Indicador Descripción
Transacciones realizadas Cantidad total de transacciones realizadas por el
hoy cajero autenticado durante el día actual.
Cantidad total de pagos a tarjetas de crédito y pagos
Pagos realizados hoy a préstamos realizados por el cajero autenticado
durante el día actual.
Cantidad total de depósitos realizados por el cajero
Depósitos realizados hoy
autenticado durante el día actual.

Indicador Descripción
Cantidad total de retiros realizados por el cajero
Retiros realizados hoy
autenticado durante el día actual.
Descripción de los indicadores
Transacciones realizadas hoy
Representa la cantidad total de operaciones realizadas por el cajero autenticado
durante la fecha actual.
Este indicador debe incluir:
● Depósitos.
● Retiros.
● Pagos a tarjetas de crédito.
● Pagos a préstamos.
● Transacciones a cuentas de terceros.
Sólo deben contarse las transacciones procesadas por el cajero actualmente
logueado.
Pagos realizados hoy
Representa la cantidad total de pagos realizados por el cajero autenticado durante
el día actual.
Para este indicador sólo deben considerarse:
● Pagos a tarjetas de crédito.
● Pagos a préstamos.
No deben contarse depósitos, retiros ni transacciones a cuentas de terceros como
pagos.
Depósitos realizados hoy
Representa la cantidad total de depósitos realizados por el cajero autenticado
durante el día actual.
Retiros realizados hoy
Representa la cantidad total de retiros realizados por el cajero autenticado durante

el día actual.
Reglas adicionales del módulo
● Solo los usuarios con rol Cajero pueden acceder al Home del cajero.
● El menú debe mostrar únicamente las opciones disponibles para el rol Cajero.
● Los indicadores deben calcularse únicamente con operaciones realizadas por el
cajero autenticado.
● Los indicadores deben tomar como referencia la fecha actual del sistema.
● Las operaciones de otros cajeros no deben incluirse en estos indicadores.
● Los pagos realizados hoy solo deben incluir pagos a tarjetas de crédito y pagos a
préstamos.
● La opción Cerrar sesión debe cerrar la sesión activa y redirigir al Login.
Depósito
Al ingresar a la opción Depósito desde el menú principal del cajero, el sistema debe
enviar al usuario a la pantalla para registrar depósitos a cuentas de ahorro.
Esta funcionalidad permitirá que el cajero acredite fondos a una cuenta de ahorro
activa registrada en el sistema.
Solo los usuarios con rol Cajero podrán acceder a esta funcionalidad. Si un usuario
con rol Administrador, Cliente o Comercio intenta acceder directamente a esta
pantalla mediante la URL, el sistema debe redirigirlo a la pantalla de Acceso
denegado.
Formulario de depósito
La pantalla debe mostrar un formulario para que el cajero pueda ingresar la cuenta
destino y el monto que desea depositar.
El formulario debe contener los siguientes campos:
Campo Tipo de dato Requerido Descripción
Número de cuenta Texto / string Sí Número de cuenta de ahorro
destino a la que se realizará el
depósito.
Monto a depositar Decimal / Sí Monto que será acreditado a
number la cuenta destino.
Debajo del formulario debe existir un botón con el texto Realizar depósito.

Descripción de campos
Número de cuenta destino
Representa la cuenta de ahorro donde será acreditado el depósito.
El número de cuenta debe corresponder a una cuenta de ahorro existente y activa.
Monto a depositar
Representa el monto que el cajero desea depositar en la cuenta destino.
Este monto debe ser mayor que cero.
Validaciones del depósito
El formulario de depósito debe cumplir las siguientes validaciones:
● El número de cuenta destino es requerido.
● El número de cuenta destino debe existir en el sistema.
● La cuenta destino debe estar activa.
● El monto a depositar es requerido.
● El monto a depositar debe ser mayor que cero.
Si el número de cuenta destino no existe o la cuenta se encuentra inactiva, el
sistema debe mostrar el siguiente mensaje:
“El número de cuenta ingresado no corresponde a una cuenta válida.”
Si el monto a depositar es menor o igual a cero, el sistema debe mostrar el siguiente
mensaje:
“El monto a depositar debe ser mayor que cero.”
Confirmación del depósito
Si todas las validaciones son correctas, el sistema debe enviar al cajero a una
pantalla de confirmación antes de ejecutar el depósito.
En esta pantalla se debe mostrar la siguiente información:
Campo Descripción
Titular de la cuenta Nombre y apellido del cliente propietario de la cuenta
destino.

Campo Descripción
Número de cuenta Número de cuenta donde se realizará el depósito.
destino
Monto a depositar Monto que será acreditado.
La pantalla debe mostrar el siguiente mensaje:
“¿Está seguro que desea realizar este depósito?”
Debajo del mensaje deben existir dos botones:
Botón Descripción
Cancelar Cancela la operación y redirige al cajero al Home del cajero.
Confirmar Ejecuta el depósito en la cuenta destino.
Si el cajero cancela la operación, el depósito no debe ejecutarse y el sistema debe
redirigirlo al Home del cajero.
Procesamiento del depósito
Si el cajero confirma la operación, el sistema debe acreditar el monto indicado a la
cuenta de ahorro destino.
El sistema debe realizar las siguientes acciones:
● Sumar el monto depositado al balance de la cuenta destino.
● Registrar la transacción en el historial de la cuenta destino como CRÉDITO.
● Asociar la operación al cajero autenticado que realizó el depósito.
● Registrar la fecha y hora exacta de la operación.
La transacción debe quedar registrada con la siguiente información:
Campo Valor
Tipo de transacción CRÉDITO
Monto Monto depositado
Origen DEPÓSITO

Campo Valor
Beneficiario Número de cuenta destino
Estado APROBADA
Usuario responsable Cajero autenticado
Fecha Fecha y hora en que se realizó la operación
Correo de depósito
Una vez procesado correctamente el depósito, el sistema debe enviar
automáticamente un correo electrónico al cliente propietario de la cuenta destino.
El asunto del correo debe ser:
“Depósito realizado a su cuenta [XXXX]”
Donde [XXXX] corresponde a los últimos cuatro dígitos del número de cuenta
destino.
El cuerpo del correo debe incluir:
● Monto depositado.
● Últimos cuatro dígitos de la cuenta destino.
● Fecha de la transacción.
● Hora exacta de la transacción.
El correo puede tener un contenido como el siguiente:
Asunto: Depósito realizado a su cuenta [XXXX]
Hola [Nombre del cliente],
Se ha realizado un depósito a su cuenta terminada en [XXXX].
Monto depositado: RD$[Monto]
Fecha y hora: [Fecha y hora]
Si usted no reconoce esta operación, comuníquese con la entidad bancaria.
Si ocurre un error al enviar el correo, el depósito no debe revertirse. El sistema debe
registrar el error y mostrar un mensaje informativo al cajero.
Mensaje sugerido:

“El depósito fue realizado correctamente, pero no fue posible enviar el correo de
notificación.”
Finalmente, una vez completada la operación, el sistema debe redirigir al cajero a su
pantalla principal, es decir, al Home del cajero.
Reglas adicionales del módulo
● Solo los usuarios con rol Cajero pueden acceder a la funcionalidad de Depósito.
● Solo se pueden realizar depósitos a cuentas de ahorro activas.
● El monto a depositar debe ser mayor que cero.
● El depósito debe registrarse como una transacción de tipo CRÉDITO.
● El origen de la transacción debe ser DEPÓSITO.
● El beneficiario de la transacción debe ser el número de cuenta destino.
● Todo depósito aprobado debe actualizar el balance de la cuenta destino.
● La operación debe quedar asociada al cajero autenticado que la realizó.
● El depósito no debe revertirse si falla el envío del correo electrónico.
● Al finalizar la operación, el sistema debe redirigir al cajero al Home del cajero.
Retiro
Al ingresar a la opción Retiro desde el menú principal del cajero, el sistema debe
enviar al usuario a la pantalla para registrar retiros desde cuentas de ahorro.
Esta funcionalidad permitirá que el cajero debite fondos de una cuenta de ahorro
activa registrada en el sistema, siempre que la cuenta exista, se encuentre activa y
tenga balance suficiente para cubrir el monto solicitado.
Solo los usuarios con rol Cajero podrán acceder a esta funcionalidad. Si un usuario
con rol Administrador, Cliente o Comercio intenta acceder directamente a esta
pantalla mediante la URL, el sistema debe redirigirlo a la pantalla de Acceso
denegado.
Formulario de retiro
La pantalla debe mostrar un formulario para que el cajero pueda ingresar la cuenta
origen y el monto que desea retirar.
El formulario debe contener los siguientes campos:

Campo Tipo de dato Requerido Descripción
Número de cuenta Texto / string Sí Número de cuenta de ahorro
origen desde la cual se desea retirar
el dinero.
Monto a retirar Decimal / Sí Monto que será debitado de
number la cuenta origen.
Debajo del formulario debe existir un botón con el texto Realizar retiro.
Descripción de campos
Número de cuenta origen
Representa la cuenta de ahorro desde la cual se descontará el dinero.
El número de cuenta debe corresponder a una cuenta de ahorro existente y activa.
Monto a retirar
Representa el monto que el cajero desea retirar desde la cuenta origen.
Este monto debe ser mayor que cero y no puede exceder el balance disponible de la
cuenta.
Validaciones del retiro
El formulario de retiro debe cumplir las siguientes validaciones:
● El número de cuenta origen es requerido.
● El número de cuenta origen debe existir en el sistema.
● La cuenta origen debe estar activa.
● El monto a retirar es requerido.
● El monto a retirar debe ser mayor que cero.
● La cuenta origen debe tener fondos suficientes para cubrir el monto indicado.
Si el número de cuenta origen no existe o la cuenta se encuentra inactiva, el sistema
debe mostrar el siguiente mensaje:
“El número de cuenta ingresado no corresponde a una cuenta válida.”
Si el monto a retirar es menor o igual a cero, el sistema debe mostrar el siguiente
mensaje:

“El monto a retirar debe ser mayor que cero.”
Si la cuenta origen no tiene fondos suficientes, el sistema debe mostrar el siguiente
mensaje:
“El monto ingresado excede el saldo disponible de la cuenta.”
Confirmación del retiro
Si todas las validaciones son correctas, el sistema debe enviar al cajero a una
pantalla de confirmación antes de ejecutar el retiro.
En esta pantalla se debe mostrar la siguiente información:
Campo Descripción
Nombre y apellido del cliente propietario de la
Titular de la cuenta
cuenta origen.
Número de cuenta origen Número de cuenta desde la cual se realizará el retiro.
Monto a retirar Monto que será debitado.
La pantalla debe mostrar el siguiente mensaje:
“¿Está seguro que desea realizar este retiro?”
Debajo del mensaje deben existir dos botones:
Botón Descripción
Cancela la operación y redirige al cajero al Home del
Cancelar
cajero.
Confirmar Ejecuta el retiro desde la cuenta origen.
Si el cajero cancela la operación, el retiro no debe ejecutarse y el sistema debe
redirigirlo al Home del cajero.
Procesamiento del retiro

Si el cajero confirma la operación, el sistema debe debitar el monto indicado de la
cuenta de ahorro origen.
El sistema debe realizar las siguientes acciones:
● Restar el monto retirado del balance de la cuenta origen.
● Registrar la transacción en el historial de la cuenta origen como DÉBITO.
● Asociar la operación al cajero autenticado que realizó el retiro.
● Registrar la fecha y hora exacta de la operación.
La transacción debe quedar registrada con la siguiente información:
Campo Valor
Tipo de transacción DÉBITO
Monto Monto retirado
Origen Número de cuenta origen
Beneficiario RETIRO
Estado APROBADA
Usuario responsable Cajero autenticado
Fecha Fecha y hora en que se realizó la operación
Si la operación es rechazada por fondos insuficientes, el sistema debe registrar el
intento como RECHAZADO en la cuenta origen, sin afectar el balance de la cuenta.
Correo de retiro
Una vez procesado correctamente el retiro, el sistema debe enviar automáticamente
un correo electrónico al cliente propietario de la cuenta origen.
El asunto del correo debe ser:
“Retiro realizado desde su cuenta [XXXX]”
Donde [XXXX] corresponde a los últimos cuatro dígitos del número de cuenta
origen.
El cuerpo del correo debe incluir:

● Monto retirado.
● Últimos cuatro dígitos de la cuenta origen.
● Fecha de la transacción.
● Hora exacta de la transacción.
El correo puede tener un contenido como el siguiente:
Asunto: Retiro realizado desde su cuenta [XXXX]
Hola [Nombre del cliente],
Se ha realizado un retiro desde su cuenta terminada en [XXXX].
Monto retirado: RD$[Monto]
Fecha y hora: [Fecha y hora]
Si usted no reconoce esta operación, comuníquese con la entidad bancaria.
Si ocurre un error al enviar el correo, el retiro no debe revertirse. El sistema debe
registrar el error y mostrar un mensaje informativo al cajero.
Mensaje sugerido:
“El retiro fue realizado correctamente, pero no fue posible enviar el correo de
notificación.”
Finalmente, una vez completada la operación, el sistema debe redirigir al cajero a su
pantalla principal, es decir, al Home del cajero.
Reglas adicionales del módulo
● Solo los usuarios con rol Cajero pueden acceder a la funcionalidad de Retiro.
● Solo se pueden realizar retiros desde cuentas de ahorro activas.
● El monto a retirar debe ser mayor que cero.
● La cuenta origen debe tener fondos suficientes antes de aprobar el retiro.
● El retiro debe registrarse como una transacción de tipo DÉBITO.
● El origen de la transacción debe ser el número de cuenta desde la cual se realizó
el retiro.
● El beneficiario de la transacción debe ser RETIRO.
● Todo retiro aprobado debe actualizar el balance de la cuenta origen.
● Las operaciones rechazadas no deben modificar el balance de la cuenta.
● La operación debe quedar asociada al cajero autenticado que la realizó.
● El retiro no debe revertirse si falla el envío del correo electrónico.
● Al finalizar la operación, el sistema debe redirigir al cajero al Home del cajero.

Pago a tarjeta de crédito
Al ingresar a la opción Pago a tarjeta de crédito desde el menú principal del cajero,
el sistema debe enviar al usuario a la pantalla para registrar pagos a tarjetas de
crédito.
Esta funcionalidad permitirá que el cajero aplique un pago a una tarjeta de crédito
activa, utilizando fondos disponibles en una cuenta de ahorro activa registrada en el
sistema.
Solo los usuarios con rol Cajero podrán acceder a esta funcionalidad. Si un usuario
con rol Administrador, Cliente o Comercio intenta acceder directamente a esta
pantalla mediante la URL, el sistema debe redirigirlo a la pantalla de Acceso
denegado.
Formulario de pago a tarjeta de crédito
La pantalla debe mostrar un formulario para que el cajero pueda ingresar la cuenta
de origen, la tarjeta destino y el monto que desea pagar.
El formulario debe contener los siguientes campos:
Campo Tipo de dato Requerido Descripción
Número de cuenta Texto / string Sí Número de cuenta de ahorro
origen desde la cual se tomará el
dinero para realizar el pago.
Número de tarjeta Texto / string Sí Número identificador de 16
de crédito dígitos de la tarjeta a la que
se aplicará el pago.
Monto a pagar Decimal / Sí Monto que se desea abonar a
number la tarjeta de crédito.
Debajo del formulario debe existir un botón con el texto Realizar pago.
Descripción de campos
Número de cuenta origen
Representa la cuenta de ahorro desde la cual se descontará el dinero para realizar
el pago.

La cuenta debe existir, estar activa y tener fondos suficientes para cubrir el monto
efectivo que será aplicado a la tarjeta.
Número de tarjeta de crédito
Representa la tarjeta de crédito a la que se aplicará el pago.
La tarjeta debe existir y estar activa.
Monto a pagar
Representa el monto que el cajero desea aplicar como pago a la tarjeta.
Este monto debe ser mayor que cero.
Si el monto ingresado supera la deuda actual de la tarjeta, el sistema solo debe
utilizar como monto efectivo de pago el valor exacto de la deuda pendiente.
Validaciones del pago a tarjeta de crédito
El formulario de pago a tarjeta de crédito debe cumplir las siguientes validaciones:
● El número de cuenta origen es requerido.
● La cuenta origen debe existir en el sistema.
● La cuenta origen debe estar activa.
● El número de tarjeta de crédito es requerido.
● El número de tarjeta debe contener 16 dígitos.
● La tarjeta de crédito debe existir en el sistema.
● La tarjeta de crédito debe estar activa.
● El monto a pagar es requerido.
● El monto a pagar debe ser mayor que cero.
● La tarjeta debe tener deuda pendiente.
● La cuenta origen debe tener fondos suficientes para cubrir el monto efectivo que
será aplicado al pago.
Si la cuenta origen no existe o se encuentra inactiva, el sistema debe mostrar el
siguiente mensaje:
“El número de cuenta ingresado no corresponde a una cuenta válida.”
Si la tarjeta de crédito no existe o se encuentra inactiva, el sistema debe mostrar el
siguiente mensaje:
“El número de tarjeta ingresado no corresponde a una tarjeta válida.”
Si el monto a pagar es menor o igual a cero, el sistema debe mostrar el siguiente

mensaje:
“El monto a pagar debe ser mayor que cero.”
Si la tarjeta no tiene deuda pendiente, el sistema debe mostrar el siguiente mensaje:
“La tarjeta seleccionada no tiene deuda pendiente.”
Si la cuenta origen no tiene fondos suficientes para cubrir el monto efectivo del
pago, el sistema debe mostrar el siguiente mensaje:
“El monto ingresado excede el saldo disponible de la cuenta.”
Regla para evitar sobrepago de tarjeta
El sistema no debe permitir pagos por encima de la deuda actual de la tarjeta.
Si el monto ingresado por el cajero excede la deuda actual de la tarjeta, solo se
debe debitar de la cuenta origen el monto correspondiente a la deuda real
pendiente.
El excedente no debe utilizarse ni descontarse de la cuenta del cliente.
Ejemplo:
Si la tarjeta tiene una deuda de RD$500.00 y el cajero intenta registrar un pago de
RD$1,000.00, el sistema solo debe debitar RD$500.00 de la cuenta origen y aplicar
RD$500.00 a la tarjeta.
El excedente de RD$500.00 no debe descontarse ni registrarse como pago.
Confirmación del pago
Si todas las validaciones son correctas, el sistema debe enviar al cajero a una
pantalla de confirmación antes de ejecutar el pago.
En esta pantalla se debe mostrar la siguiente información:
Campo Descripción
Titular de la cuenta Nombre y apellido del cliente propietario de la cuenta
origen desde la cual se tomará el dinero.
Número de cuenta origen Número de cuenta desde la cual se realizará el débito.

Campo Descripción
Titular de la tarjeta Nombre y apellido del cliente propietario de la tarjeta
de crédito.
Tarjeta destino Últimos cuatro dígitos de la tarjeta a la que se
aplicará el pago.
Monto ingresado Monto digitado por el cajero.
Monto efectivo a pagar Monto que realmente será debitado y aplicado a la
tarjeta.
La pantalla debe mostrar el siguiente mensaje:
“¿Está seguro que desea realizar este pago?”
Debajo del mensaje deben existir dos botones:
Botón Descripción
Cancelar Cancela la operación y redirige al cajero al Home del cajero.
Confirmar Ejecuta el pago a la tarjeta de crédito.
Si el cajero cancela la operación, el pago no debe ejecutarse y el sistema debe
redirigirlo al Home del cajero.
Procesamiento del pago
Si el cajero confirma la operación, el sistema debe aplicar el pago a la tarjeta de
crédito.
El sistema debe realizar las siguientes acciones:
● Debitar el monto efectivo de pago desde la cuenta de ahorro origen.
● Reducir la deuda de la tarjeta de crédito por el monto efectivo pagado.
● Actualizar el crédito disponible de la tarjeta.
● Registrar la transacción en el historial de la cuenta origen como DÉBITO.
● Asociar la operación al cajero autenticado que realizó el pago.
● Registrar la fecha y hora exacta de la operación.
La transacción debe quedar registrada con la siguiente información:

Campo Valor
Tipo de transacción DÉBITO
Monto Monto efectivo pagado
Origen
Número de cuenta origen
Beneficiario Últimos cuatro dígitos de la tarjeta pagada
Estado APROBADA
Usuario responsable Cajero autenticado
Fecha Fecha y hora en que se realizó la operación
El sistema puede guardar internamente la referencia completa de la tarjeta para
fines de trazabilidad, pero en los listados y comprobantes solo deben mostrarse los
últimos cuatro dígitos.
Si la operación es rechazada por fondos insuficientes o por alguna validación de
negocio, el sistema debe registrar el intento como RECHAZADO en la cuenta
origen, sin afectar el balance de la cuenta ni la deuda de la tarjeta.
Correo de pago a tarjeta de crédito
Una vez procesado correctamente el pago, el sistema debe enviar automáticamente
un correo electrónico notificando la operación.
El correo debe enviarse al cliente propietario de la tarjeta de crédito.
Si el propietario de la cuenta origen es diferente al propietario de la tarjeta, también
debe enviarse una notificación al propietario de la cuenta origen, indicando que se
debitó dinero de su cuenta para realizar el pago.
El asunto del correo para el propietario de la tarjeta debe ser:
“Pago realizado a la tarjeta [XXXX]”
Donde [XXXX] corresponde a los últimos cuatro dígitos del número de tarjeta.
El cuerpo del correo debe incluir:
● Monto pagado.

● Últimos cuatro dígitos de la cuenta desde la cual se realizó el pago.
● Últimos cuatro dígitos de la tarjeta pagada.
● Fecha de la transacción.
● Hora exacta de la transacción.
El correo puede tener un contenido como el siguiente:
Asunto: Pago realizado a la tarjeta [XXXX]
Hola [Nombre del cliente],
Se ha realizado un pago a su tarjeta de crédito terminada en [XXXX].
Monto pagado: RD$[Monto]
Cuenta origen terminada en: [XXXX]
Fecha y hora: [Fecha y hora]
Si usted no reconoce esta operación, comuníquese con la entidad bancaria.
Si ocurre un error al enviar el correo, el pago no debe revertirse. El sistema debe
registrar el error y mostrar un mensaje informativo al cajero.
Mensaje sugerido:
“El pago fue realizado correctamente, pero no fue posible enviar el correo de
notificación.”
Finalmente, una vez completada la operación, el sistema debe redirigir al cajero a su
pantalla principal, es decir, al Home del cajero.
Reglas adicionales del módulo
● Solo los usuarios con rol Cajero pueden acceder a la funcionalidad de Pago a
tarjeta de crédito.
● Solo se pueden realizar pagos desde cuentas de ahorro activas.
● Solo se pueden aplicar pagos a tarjetas de crédito activas.
● El monto a pagar debe ser mayor que cero.
● La tarjeta debe tener deuda pendiente para poder recibir un pago.
● El sistema no debe permitir sobrepagos a tarjetas de crédito.
● Si el monto ingresado excede la deuda actual, solo se debe debitar el monto
correspondiente a la deuda real.
● El pago debe registrarse como una transacción de tipo DÉBITO en la cuenta
origen.
● El beneficiario de la transacción debe identificar la tarjeta pagada mostrando
únicamente sus últimos cuatro dígitos.

● Todo pago aprobado debe actualizar el balance de la cuenta origen, la deuda de
la tarjeta y el crédito disponible.
● Las operaciones rechazadas no deben modificar balances ni deudas.
● La operación debe quedar asociada al cajero autenticado que la realizó.
● El pago no debe revertirse si falla el envío del correo electrónico.
● Al finalizar la operación, el sistema debe redirigir al cajero al Home del cajero.
Pago a préstamo
Al ingresar a la opción Pago a préstamo desde el menú principal del cajero, el
sistema debe enviar al usuario a la pantalla para registrar pagos a préstamos.
Esta funcionalidad permitirá que el cajero aplique un pago a un préstamo activo,
utilizando fondos disponibles en una cuenta de ahorro activa registrada en el
sistema.
Solo los usuarios con rol Cajero podrán acceder a esta funcionalidad. Si un usuario
con rol Administrador, Cliente o Comercio intenta acceder directamente a esta
pantalla mediante la URL, el sistema debe redirigirlo a la pantalla de Acceso
denegado.
Formulario de pago a préstamo
La pantalla debe mostrar un formulario para que el cajero pueda ingresar la cuenta
de origen, el préstamo destino y el monto que desea pagar.
El formulario debe contener los siguientes campos:
Campo Tipo de dato Requerido Descripción
Número de cuenta de ahorro
Número de cuenta
Texto / string Sí desde la cual se tomará el
origen
dinero para realizar el pago.
Número identificador de 9
Número del
Texto / string Sí dígitos del préstamo al que
préstamo
se aplicará el pago.
Decimal / Monto que se desea abonar
Monto a pagar Sí
number al préstamo.
Debajo del formulario debe existir un botón con el texto Realizar pago.

Descripción de campos
Número de cuenta origen
Representa la cuenta de ahorro desde la cual se descontará el dinero para realizar
el pago.
La cuenta debe existir, estar activa y tener fondos suficientes para cubrir el monto
efectivo que será aplicado al préstamo.
Número del préstamo
Representa el préstamo al que se aplicará el pago.
El préstamo debe existir y debe encontrarse activo. No se deben permitir pagos a
préstamos completados.
Monto a pagar
Representa el monto que el cajero desea aplicar como pago al préstamo.
Este monto debe ser mayor que cero.
Si el monto ingresado supera el monto total pendiente del préstamo, el sistema
solo debe utilizar como monto efectivo de pago el valor exacto de la deuda
pendiente.
Validaciones del pago a préstamo
El formulario de pago a préstamo debe cumplir las siguientes validaciones:
● El número de cuenta origen es requerido.
● La cuenta origen debe existir en el sistema.
● La cuenta origen debe estar activa.
● El número del préstamo es requerido.
● El número del préstamo debe contener 9 dígitos.
● El préstamo debe existir en el sistema.
● El préstamo debe estar activo.
● El préstamo debe tener cuotas pendientes de pago.
● El monto a pagar es requerido.
● El monto a pagar debe ser mayor que cero.
● La cuenta origen debe tener fondos suficientes para cubrir el monto efectivo que
será aplicado al préstamo.
Si la cuenta origen no existe o se encuentra inactiva, el sistema debe mostrar el
siguiente mensaje:

“El número de cuenta ingresado no corresponde a una cuenta válida.”
Si el préstamo no existe o se encuentra completado, el sistema debe mostrar el
siguiente mensaje:
“El número de préstamo ingresado no corresponde a un préstamo válido.”
Si el monto a pagar es menor o igual a cero, el sistema debe mostrar el siguiente
mensaje:
“El monto a pagar debe ser mayor que cero.”
Si el préstamo no tiene cuotas pendientes, el sistema debe mostrar el siguiente
mensaje:
“El préstamo seleccionado no tiene cuotas pendientes de pago.”
Si la cuenta origen no tiene fondos suficientes para cubrir el monto efectivo del
pago, el sistema debe mostrar el siguiente mensaje:
“El monto ingresado excede el saldo disponible de la cuenta.”
Regla para evitar pagos superiores a la deuda del préstamo
El sistema no debe descontar de la cuenta de ahorro un monto mayor al total
pendiente real del préstamo.
Si el monto ingresado por el cajero excede el monto pendiente del préstamo, solo
se debe debitar de la cuenta origen el monto correspondiente a la deuda real
pendiente.
El excedente no debe utilizarse ni descontarse de la cuenta del cliente.
Ejemplo:
Si el préstamo tiene un monto pendiente de RD$2,000.00 y el cajero intenta
registrar un pago de RD$3,000.00, el sistema solo debe debitar RD$2,000.00 de la
cuenta origen y aplicar RD$2,000.00 al préstamo.
El excedente de RD$1,000.00 no debe descontarse ni registrarse como pago.
Confirmación del pago
Si todas las validaciones son correctas, el sistema debe enviar al cajero a una
pantalla de confirmación antes de ejecutar el pago.
En esta pantalla se debe mostrar la siguiente información:

Campo Descripción
Titular de la cuenta Nombre y apellido del cliente propietario de la cuenta
origen desde la cual se tomará el dinero.
Número de cuenta origen Número de cuenta desde la cual se realizará el débito.
Nombre y apellido del cliente propietario del
Titular del préstamo
préstamo.
Número identificador del préstamo al que se aplicará
Número del préstamo
el pago.
Monto ingresado Monto digitado por el cajero.
Monto que realmente será debitado y aplicado al
Monto efectivo a pagar
préstamo.
La pantalla debe mostrar el siguiente mensaje:
“¿Está seguro que desea realizar este pago?”
Debajo del mensaje deben existir dos botones:
Botón Descripción
Cancela la operación y redirige al cajero al Home del
Cancelar
cajero.
Confirmar Ejecuta el pago del préstamo.
Si el cajero cancela la operación, el pago no debe ejecutarse y el sistema debe
redirigirlo al Home del cajero.
Aplicación del pago al préstamo
Si el cajero confirma la operación, el sistema debe aplicar el pago siguiendo la tabla
de amortización del préstamo.
El sistema debe buscar la primera cuota pendiente, es decir, la cuota más cercana
en fecha que no haya sido pagada completamente o que tenga saldo pendiente,
aunque sea parcial.

El monto efectivo pagado debe aplicarse de la siguiente manera:
● Si el monto alcanza para completar la primera cuota pendiente, esa cuota debe
marcarse como pagada.
● Si el monto no alcanza para completar la primera cuota pendiente, esa cuota
debe quedar parcialmente pagada.
● Si después de pagar una cuota queda saldo disponible del monto efectivo
pagado, el sistema debe continuar aplicando el excedente a la siguiente cuota
pendiente.
● Este proceso debe repetirse hasta que el monto efectivo pagado se agote o hasta
que no existan más cuotas pendientes.
Si todas las cuotas del préstamo quedan pagadas, el préstamo debe actualizarse al
estado Completado.
Si una cuota atrasada es pagada completamente, el sistema debe actualizar su
indicador de atraso para que ya no aparezca como atrasada.
Procesamiento del pago
Cuando el pago sea aprobado, el sistema debe realizar las siguientes acciones:
● Debitar el monto efectivo de pago desde la cuenta de ahorro origen.
● Aplicar el pago al préstamo siguiendo el orden de la tabla de amortización.
● Actualizar el estado de las cuotas afectadas.
● Actualizar el monto pendiente del préstamo.
● Marcar el préstamo como Completado si todas sus cuotas quedan pagadas.
● Registrar la transacción en el historial de la cuenta origen como DÉBITO.
● Asociar la operación al cajero autenticado que realizó el pago.
● Registrar la fecha y hora exacta de la operación.
La transacción debe quedar registrada con la siguiente información:
Campo Valor
Tipo de transacción DÉBITO
Monto Monto efectivo pagado
Origen Número de cuenta origen
Beneficiario Número identificador del préstamo
Estado APROBADA

Campo Valor
Usuario responsable Cajero autenticado
Fecha Fecha y hora en que se realizó la operación
Si la operación es rechazada por fondos insuficientes o por alguna validación de
negocio, el sistema debe registrar el intento como RECHAZADO en la cuenta
origen, sin afectar el balance de la cuenta ni las cuotas del préstamo.
Correo de pago a préstamo
Una vez procesado correctamente el pago, el sistema debe enviar automáticamente
un correo electrónico notificando la operación.
El correo debe enviarse al cliente propietario del préstamo.
Si el propietario de la cuenta origen es diferente al propietario del préstamo,
también debe enviarse una notificación al propietario de la cuenta origen, indicando
que se debitó dinero de su cuenta para realizar el pago.
El asunto del correo para el propietario del préstamo debe ser:
“Pago realizado al préstamo [XXXXXXXXX]”
Donde [XXXXXXXXX] corresponde al número identificador de 9 dígitos del
préstamo.
El cuerpo del correo debe incluir:
● Monto pagado.
● Número del préstamo.
● Últimos cuatro dígitos de la cuenta desde la cual se realizó el pago.
● Fecha de la transacción.
● Hora exacta de la transacción.
El correo puede tener un contenido como el siguiente:
Asunto: Pago realizado al préstamo [XXXXXXXXX]
Hola [Nombre del cliente],
Se ha realizado un pago a su préstamo [XXXXXXXXX].
Monto pagado: RD$[Monto]
Cuenta origen terminada en: [XXXX]

Fecha y hora: [Fecha y hora]
Si usted no reconoce esta operación, comuníquese con la entidad bancaria.
Si ocurre un error al enviar el correo, el pago no debe revertirse. El sistema debe
registrar el error y mostrar un mensaje informativo al cajero.
Mensaje sugerido:
“El pago fue realizado correctamente, pero no fue posible enviar el correo de
notificación.”
Finalmente, una vez completada la operación, el sistema debe redirigir al cajero a su
pantalla principal, es decir, al Home del cajero.
Reglas adicionales del módulo
● Solo los usuarios con rol Cajero pueden acceder a la funcionalidad de Pago a
préstamo.
● Solo se pueden realizar pagos desde cuentas de ahorro activas.
● Solo se pueden aplicar pagos a préstamos activos.
● El monto a pagar debe ser mayor que cero.
● El préstamo debe tener cuotas pendientes para poder recibir un pago.
● El sistema no debe descontar montos superiores al total pendiente real del
préstamo.
● Si el monto ingresado excede la deuda pendiente, solo se debe debitar el monto
correspondiente a la deuda real.
● El pago debe aplicarse desde la cuota pendiente más antigua hacia las cuotas
siguientes.
● Una cuota puede quedar parcialmente pagada si el monto aplicado no alcanza
para saldar completa.
● El pago debe registrarse como una transacción de tipo DÉBITO en la cuenta
origen.
● El beneficiario de la transacción debe ser el número identificador del préstamo.
● Todo pago aprobado debe actualizar el balance de la cuenta origen, las cuotas
afectadas y el monto pendiente del préstamo.
● Si todas las cuotas quedan pagadas, el préstamo debe pasar al estado completo.
● Las operaciones rechazadas no deben modificar balances ni cuotas.
● La operación debe quedar asociada al cajero autenticado que la realizó.
● El pago no debe revertirse si falla el envío del correo electrónico.
● Al finalizar la operación, el sistema debe redirigir al cajero al Home del cajero.

Transacciones a cuentas de terceros
Al ingresar a la opción Transacciones a cuentas de terceros desde el menú principal
del cajero, el sistema debe enviar al usuario a la pantalla para realizar transferencias
entre cuentas de ahorro registradas en el sistema.
Esta funcionalidad permitirá que el cajero debite fondos desde una cuenta de ahorro
origen y los acredite en una cuenta de ahorro destino, siempre que ambas cuentas
existan, estén activas y la cuenta origen tenga fondos suficientes.
Solo los usuarios con rol Cajero podrán acceder a esta funcionalidad. Si un usuario
con rol Administrador, Cliente o Comercio intenta acceder directamente a esta
pantalla mediante la URL, el sistema debe redirigirlo a la pantalla de Acceso
denegado.
Formulario de transacción a cuenta de terceros
La pantalla debe mostrar un formulario para que el cajero pueda ingresar la cuenta
de origen, la cuenta destino y el monto de la transacción.
El formulario debe contener los siguientes campos:
| Campo          | Tipo de dato    | Requerido  |                                 | Descripción  |
| -------------- | --------------- | ---------- | ------------------------------- | ------------ |
| Número de      | Texto / string  | Sí         | Número de cuenta de ahorro      |              |
| cuenta origen  |                 |            | desde la cual se descontará el  |              |
dinero.
| Número de       | Texto / string  | Sí  | Número de cuenta de ahorro       |     |
| --------------- | --------------- | --- | -------------------------------- | --- |
| cuenta destino  |                 |     | donde se acreditará el dinero.   |     |
| Monto de la     | Decimal /       | Sí  | Monto que será transferido       |     |
| transacción     | number          |     | desde la cuenta origen hacia la  |     |
cuenta destino.
Debajo del formulario debe existir un botón con el texto Realizar transacción.
Descripción de campos
Número de cuenta origen
Representa la cuenta de ahorro desde la cual se descontará el dinero.
La cuenta debe existir, estar activa y tener fondos suficientes para cubrir el monto
indicado.

Número de cuenta destino
Representa la cuenta de ahorro donde será acreditado el dinero.
La cuenta debe existir y estar activa.
Monto de la transacción
Representa la cantidad de dinero que será transferida desde la cuenta origen hacia
la cuenta destino.
Este monto debe ser mayor que cero.
Validaciones de la transacción
El formulario de transacción a cuentas de terceros debe cumplir las siguientes
validaciones:
● El número de cuenta origen es requerido.
● El número de cuenta origen debe existir en el sistema.
● La cuenta origen debe estar activa.
● El número de cuenta destino es requerido.
● El número de cuenta destino debe existir en el sistema.
● La cuenta destino debe estar activa.
● La cuenta origen y la cuenta destino no pueden ser la misma.
● El monto de la transacción es requerido.
● El monto de la transacción debe ser mayor que cero.
● La cuenta origen debe tener fondos suficientes para cubrir el monto indicado.
Si la cuenta origen no existe o se encuentra inactiva, el sistema debe mostrar el
siguiente mensaje:
“El número de cuenta origen ingresado no corresponde a una cuenta válida.”
Si la cuenta destino no existe o se encuentra inactiva, el sistema debe mostrar el
siguiente mensaje:
“El número de cuenta destino ingresado no corresponde a una cuenta válida.”
Si la cuenta origen y la cuenta destino son la misma, el sistema debe mostrar el
siguiente mensaje:
“La cuenta origen y la cuenta destino no pueden ser la misma.”
Si el monto ingresado es menor o igual a cero, el sistema debe mostrar el siguiente
mensaje:

“El monto de la transacción debe ser mayor que cero.”
Si la cuenta origen no tiene fondos suficientes, el sistema debe mostrar el siguiente
mensaje:
“El monto ingresado excede el saldo disponible de la cuenta.”
Confirmación de la transacción
Si todas las validaciones son correctas, el sistema debe enviar al cajero a una
pantalla de confirmación antes de ejecutar la transacción.
En esta pantalla se debe mostrar la siguiente información:
Campo Descripción
Titular de la cuenta Nombre y apellido del cliente propietario de la
origen cuenta desde la cual se descontará el dinero.
Número de cuenta desde la cual se realizará el
Número de cuenta origen
débito.
Titular de la cuenta Nombre y apellido del cliente propietario de la
destino cuenta que recibirá el dinero.
Número de cuenta donde se acreditará el dinero.
Número de cuenta
destino
Monto de la transacción Monto que será transferido.
La pantalla debe mostrar el siguiente mensaje:
“¿Está seguro de que desea realizar esta transacción?”
Debajo del mensaje deben existir dos botones:
Botón Descripción
Cancela la operación y redirige al cajero al Home del
Cancelar
cajero.

Botón Descripción
Confirmar Ejecuta la transacción entre las cuentas indicadas.
Si el cajero cancela la operación, la transacción no debe ejecutarse y el sistema debe
redirigirlo al Home del cajero.
Procesamiento de la transacción
Si el cajero confirma la operación, el sistema debe debitar el monto indicado de la
cuenta de ahorro origen y acreditar el mismo monto en la cuenta de ahorro destino.
El sistema debe realizar las siguientes acciones:
● Restar el monto de la transacción del balance de la cuenta origen.
● Sumar el mismo monto al balance de la cuenta destino.
● Registrar la transacción en la cuenta origen como DÉBITO.
● Registrar la transacción en la cuenta destino como CRÉDITO.
● Asociar la operación al cajero autenticado que realizó la transacción.
● Registrar la fecha y hora exacta de la operación.
La operación debe ejecutarse de forma transaccional. Si ocurre un error al debitar,
acreditar o registrar alguno de los movimientos, el sistema no debe aplicar
parcialmente la transacción.
Registro en la cuenta origen
En la cuenta de origen, la transacción debe registrarse como un movimiento de tipo
DÉBITO, ya que representa una salida de fondos.
La transacción debe quedar registrada con la siguiente información:
Campo Valor
Tipo de transacción DÉBITO
Monto Monto transferido
Origen Número de cuenta origen
Beneficiario Número de cuenta destino
Estado APROBADA

Campo Valor
Usuario responsable Cajero autenticado
Fecha Fecha y hora en que se realizó la operación
Registro en la cuenta destino
En la cuenta destino, la transacción debe registrarse como un movimiento de tipo
CRÉDITO, ya que representa una entrada de fondos.
La transacción debe quedar registrada con la siguiente información:
Campo Valor
Tipo de transacción CRÉDITO
Monto Monto recibido
Origen Número de cuenta origen
Beneficiario Número de cuenta destino
Estado APROBADA
Usuario responsable Cajero autenticado
Fecha Fecha y hora en que se realizó la operación
Este registro cruzado garantiza la trazabilidad de la operación, permitiendo
identificar desde cuál cuenta salió el dinero, hacia cuál cuenta fue enviado y qué
cajero realizó la transacción.
Si la operación es rechazada por fondos insuficientes o por alguna validación de
negocio, el sistema debe registrar el intento como RECHAZADO en la cuenta origen
cuando ésta exista, sin afectar ningún balance.
Correos de notificación
Una vez procesada correctamente la transacción, el sistema debe enviar
automáticamente dos correos electrónicos.
El primer correo debe enviarse al cliente propietario de la cuenta origen, notificando
que se ha realizado un envío de dinero hacia otra cuenta.

El asunto del correo debe ser:
“Transacción realizada a la cuenta [XXXX]”
Donde [XXXX] corresponde a los últimos cuatro dígitos del número de cuenta
destino.
El cuerpo del correo debe incluir:
● Monto transferido.
● Últimos cuatro dígitos de la cuenta origen.
● Últimos cuatro dígitos de la cuenta destino.
● Fecha de la transacción.
● Hora exacta de la transacción.
El segundo correo debe enviarse al cliente propietario de la cuenta destino,
notificando que ha recibido una transacción desde otra cuenta.
El asunto del correo debe ser:
“Transacción enviada desde la cuenta [XXXX]”
Donde [XXXX] corresponde a los últimos cuatro dígitos del número de cuenta
origen.
El cuerpo del correo debe incluir:
● Monto recibido.
● Últimos cuatro dígitos de la cuenta origen.
● Últimos cuatro dígitos de la cuenta destino.
● Fecha de la transacción.
● Hora exacta de la transacción.
Si ocurre un error al enviar uno o ambos correos, la transacción no debe revertirse.
El sistema debe registrar el error y mostrar un mensaje informativo al cajero.
Mensaje sugerido:
“La transacción fue realizada correctamente, pero no fue posible enviar una o más
notificaciones por correo.”
Finalmente, una vez completada la operación, el sistema debe redirigir al cajero a su
pantalla principal, es decir, al Home del cajero.
Reglas adicionales del módulo

● Solo los usuarios con rol Cajero pueden acceder a la funcionalidad de
Transacciones a cuentas de terceros.
● Solo se pueden realizar transacciones entre cuentas de ahorro activas.
● La cuenta origen y la cuenta destino deben existir en el sistema.
● La cuenta origen y la cuenta destino no pueden ser la misma cuenta.
● El monto de la transacción debe ser mayor que cero.
● La cuenta origen debe tener fondos suficientes antes de aprobar la transacción.
● La cuenta origen debe registrar la operación como DÉBITO.
● La cuenta destino debe registrar la operación como CRÉDITO.
● Todo movimiento aprobado debe actualizar los balances de ambas cuentas.
● Las operaciones rechazadas no deben modificar balances.
● La operación debe quedar asociada al cajero autenticado que la realizó.
● La transacción debe ejecutarse de forma transaccional para evitar inconsistencias.
● El envío de correos no debe revertir la transacción si ocurre un error.
● Al finalizar la operación, el sistema debe redirigir al cajero al Home del cajero.
Funcionalidades del API
Seguridad
La Web API debe implementar autenticación y autorización utilizando JWT, con el
objetivo de proteger todos los endpoints internos del sistema.
La seguridad de la API debe estar basada en roles, de manera que cada usuario
pueda acceder únicamente a los endpoints correspondientes a su rol.
La API manejará los siguientes roles:
Rol Descripción
Usuario con acceso a las funcionalidades administrativas
Administrador
de la API.
Usuario con acceso a las funcionalidades de comercio y
Comercio
procesamiento de pagos mediante Hermes Pay.
Los roles utilizados en la Web API serán independientes del acceso a la aplicación
web. El rol Comercio no debe tener acceso a la aplicación web MVC y debe
utilizarse únicamente para las funcionalidades expuestas por la API.
Autenticación mediante JWT

Los endpoints protegidos de la API deben requerir un token JWT válido para poder
ser utilizados.
Cuando un usuario inicie sesión correctamente desde la API, el sistema debe
generar un token JWT que incluya como mínimo la siguiente información:
● Identificador del usuario.
● Nombre de usuario.
● Rol del usuario.
● Fecha de emisión del token.
● Fecha de expiración del token.
El token JWT debe enviarse en cada solicitud protegida utilizando el encabezado de
autorización correspondiente.
Formato esperado:
Authorization: Bearer [TOKEN]
Si el token no es enviado, es inválido o está expirado, el sistema debe rechazar la
solicitud.
En ese caso, la API debe responder con el código de estado 401 Unauthorized y un
mensaje como el siguiente:
“No tiene autorización para acceder a este recurso.”
Autorización por rol
Además de validar que el usuario esté autenticado, la API debe validar que el
usuario tenga el rol requerido para acceder a cada endpoint.
Los endpoints administrativos solo deben estar disponibles para usuarios con rol
Administrador.
Los endpoints de comercio solo deben estar disponibles para usuarios con rol
Comercio.
Si un usuario autenticado intenta acceder a un endpoint que no corresponde a su
rol, la API debe rechazar la solicitud.
En ese caso, la API debe responder con el código de estado 403 Forbidden y un
mensaje como el siguiente:
“Acceso denegado. No tiene permisos para utilizar este recurso.”

Reglas de acceso
Las reglas de acceso de la API deben funcionar de la siguiente manera:
Condición Resultado esperado
Usuario sin token JWT No puede acceder a endpoints protegidos. Debe
recibir respuesta 401.
Usuario con token inválido No puede acceder a endpoints protegidos. Debe
recibir respuesta 401.
Usuario con token expirado No puede acceder a endpoints protegidos. Debe
recibir respuesta 401.
Usuario Administrador No debe tener acceso. Debe recibir respuesta
intentando acceder a endpoints 403.
de Comercio
Usuario Comercio intentando No debe tener acceso. Debe recibir respuesta
acceder a endpoints de 403.
Administrador
Usuario autenticado con el rol Puede acceder al endpoint solicitado.
correcto
Activación de usuarios de la API
Los usuarios creados en el sistema deben quedar inicialmente inactivos y deben
completar un proceso de confirmación de cuenta antes de poder iniciar sesión.
Cuando se cree un usuario de la API, el sistema debe enviar un correo electrónico
con un enlace o token de activación.
El usuario no debe poder autenticarse ni generar un JWT hasta que su cuenta haya
sido activada correctamente.
Si un usuario intenta iniciar sesión y su cuenta se encuentra inactiva, la API debe

rechazar la solicitud y responder con un mensaje como el siguiente:
“Su cuenta se encuentra inactiva. Debe activar su cuenta antes de iniciar sesión.”
Usuarios creados por defecto
El sistema debe crear mediante seeding los roles principales de la API.
Los roles creados por defecto deben ser:
● Administrador.
● Comercio.
Además, el sistema debe crear un usuario por defecto para cada uno de estos roles:
Usuario por defecto Rol Estado inicial
Usuario administrador de API Administrador Activo
Usuario comercio de API Comercio Activo
Estos usuarios por defecto deben crearse activos, ya que serán utilizados para
acceder inicialmente a la API y probar sus funcionalidades principales.
Los usuarios creados posteriormente desde los endpoints o módulos
correspondientes deben quedar inicialmente inactivos y deben completar el proceso
de activación de cuenta.
Implementación de seguridad
Todas las validaciones de autenticación y autorización deben implementarse
utilizando ASP.NET Identity, JWT Bearer Authentication y los filtros de autorización
de ASP.NET Core.
Los endpoints protegidos deben utilizar filtros como Authorize.
Los endpoints que dependan de un rol específico deben utilizar autorización por rol.
Ejemplo de protección esperada:
● Endpoints administrativos: solo rol Administrador.
● Endpoints de comercio: solo rol Comercio.
● Endpoints públicos, como login o activación de cuenta: no deben requerir token

JWT.
La seguridad no debe depender de validaciones manuales dentro de cada endpoint
cuando pueda resolverse mediante filtros de autorización.
Respuestas esperadas de seguridad
Cuando una solicitud no autorizada o no autenticada sea rechazada, la API debe
devolver una respuesta clara y consistente.
Escenario Código HTTP Mensaje sugerido
Usuario no autenticado 401 “No tiene autorización para acceder
Unauthorized a este recurso.”
Token inválido o expirado 401 “No tiene autorización para acceder
Unauthorized a este recurso.”
Usuario autenticado sin 403 Forbidden “Acceso denegado. No tiene
permisos para el permisos para utilizar este
endpoint recurso.”
Usuario inactivo 400 Bad “Su cuenta se encuentra inactiva.
intentando iniciar sesión Request o 401 Debe activar su cuenta antes de
Unauthorized iniciar sesión.”
Reglas adicionales de seguridad del API
● La API debe utilizar JWT para proteger sus endpoints internos.
● Los endpoints protegidos no deben permitir acceso sin token válido.
● Un usuario no autenticado debe recibir una respuesta 401.
● Un usuario autenticado que no tenga el rol requerido debe recibir una respuesta
403.
● Los usuarios con rol Administrador no deben acceder a endpoints exclusivos de
Comercio.
● Los usuarios con rol Comercio no deben acceder a endpoints exclusivos de
Administrador.
● Los roles Administrador y Comercio deben crearse mediante seeding.
● Debe existir un usuario por defecto para cada rol de la API.
● Los usuarios por defecto deben estar activos para permitir las pruebas iniciales.

● Los usuarios creados posteriormente deben iniciar inactivos hasta completar la
activación de cuenta.
● Los endpoints de login y activación de cuenta deben estar disponibles sin token
JWT.
● Los endpoints protegidos deben utilizar filtros de autorización de ASP.NET Core.
Módulo: Login y Account Controller
Este módulo permite realizar las operaciones generales de autenticación y
administración inicial de cuentas para los usuarios de la Web API.
A través de este módulo, los usuarios podrán iniciar sesión, obtener un token JWT,
confirmar su cuenta, solicitar un token para restablecer contraseña y cambiar su
contraseña utilizando dicho token.
Seguridad del módulo
Los endpoints de este módulo forman parte del flujo de autenticación de la API. Por
esta razón, los siguientes endpoints no deben requerir un token JWT previo:
● POST /account/login
● POST /account/confirm
● POST /account/get-reset-token
● POST /account/reset-password
Esto es necesario porque un usuario que todavía no ha confirmado su cuenta o que
olvidó su contraseña no podrá tener un JWT válido antes de completar estos
procesos.
Los demás endpoints protegidos de la API sí deben requerir autenticación JWT
utilizando el siguiente encabezado:
Authorization: Bearer {token_jwt}
Los usuarios que inicien sesión correctamente deben pertenecer a uno de los roles
permitidos por la API:
● Administrador.
● Comercio.
Si el usuario existe, pero se encuentra inactivo, no se debe generar token JWT hasta
que complete el proceso de confirmación o restablecimiento correspondiente.

Login
Endpoint
POST /account/login
Descripción
Permite autenticar un usuario de la API y obtener un token JWT para consumir los
endpoints protegidos del sistema.
Este endpoint podrá ser utilizado por usuarios con rol Administrador o Comercio,
siempre que sus credenciales sean válidas y su cuenta se encuentre activa.
Request Body
{
"userName": "admin",
"password": "123P@$$word!"
}
Campos del body
Campo Tipo de dato Requerido Descripción
userName string Sí Nombre de usuario registrado en el
sistema.
password string Sí Contraseña asociada al usuario.
Validaciones
● El campo userName es obligatorio.
● El campo password es obligatorio.
● El usuario debe existir en el sistema.
● La contraseña debe coincidir con la registrada para el usuario.
● El usuario debe estar activo.
● El usuario debe tener rol Administrador o Comercio.
Si el usuario no existe o la contraseña es incorrecta, el sistema debe responder con
401 Unauthorized.
Si el usuario existe, pero se encuentra inactivo, el sistema debe responder con 401

Unauthorized e indicar que la cuenta debe ser activada.
Si el usuario tiene un rol no permitido para la API, el sistema debe responder con
403 Forbidden.
Respuestas
Código HTTP Resultado Descripción
200 OK Token generado Retorna el token JWT del usuario
autenticado.
400 Bad Request Solicitud inválida Faltan uno o más parámetros
requeridos.
401 Unauthorized Credenciales Usuario o contraseña incorrectos, o
inválidas cuenta inactiva.
403 Forbidden Acceso denegado El usuario no tiene un rol permitido
para usar la API.
Respuesta 200 OK
{
"jwt": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
Confirmar cuenta
Endpoint
POST /account/confirm
Descripción
Permite confirmar y activar una cuenta de usuario mediante un token de
confirmación enviado previamente por correo electrónico.
Este endpoint no debe requerir JWT, ya que se utiliza precisamente para activar
usuarios que todavía no pueden iniciar sesión.
Request Body

{
"token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9"
}
Campos del body
Campo Tipo de dato Requerido Descripción
token string Sí Token de confirmación
enviado al correo del usuario.
Validaciones
● El token es obligatorio.
● El token debe existir.
● El token debe ser válido.
● El token no debe haber sido utilizado anteriormente.
● El token debe estar asociado a un usuario existente.
● El usuario asociado debe encontrarse inactivo.
Si el token es válido, el sistema debe activar la cuenta del usuario y marcar el token
como utilizado.
Si el token es inválido, está vacío, ya fue utilizado o no pertenece a un usuario
válido, el sistema debe responder con 400 Bad Request.
Respuestas
Código HTTP Resultado Descripción
Cuenta
204 No Content El usuario fue activado correctamente.
confirmada
400 Bad Solicitud Token vacío, inválido, expirado, utilizado o
Request inválida no asociado a un usuario válido.
Obtener token para reseteo de password
Endpoint

POST /account/get-reset-token
Descripción
Permite solicitar un token para restablecer la contraseña de un usuario.
El sistema debe generar un token de restablecimiento y enviarlo al correo
electrónico registrado del usuario.
Este endpoint no debe requerir JWT, ya que puede ser utilizado por usuarios que
olvidaron su contraseña y no pueden iniciar sesión.
Request Body
{
"userName": "admin"
}
Campos del body
Campo Tipo de dato Requerido Descripción
Nombre de usuario de la cuenta
userName string Sí que solicita el restablecimiento de
contraseña.
Validaciones
● El campo userName es obligatorio.
● El usuario debe existir en el sistema.
● El usuario debe tener un correo electrónico registrado.
● El usuario debe pertenecer a uno de los roles permitidos por la API:
Administrador o Comercio.
Si el usuario es válido, el sistema debe realizar las siguientes acciones:
● Inactivar temporalmente la cuenta del usuario.
● Generar un token de restablecimiento de contraseña.
● Asociar el token al usuario correspondiente.
● Guardar la fecha de generación del token.
● Enviar un correo electrónico al usuario con el token generado.
El correo enviado desde la API no debe contener un enlace de restablecimiento.
Debe incluir el token directamente en el cuerpo del correo, para que pueda ser

utilizado posteriormente en el endpoint de reseteo de contraseña.
Contenido sugerido del correo
Asunto: Token de restablecimiento de contraseña
Hola [Nombre del usuario],
Se ha generado un token para restablecer la contraseña de su cuenta.
Token de restablecimiento:
[TOKEN]
Utilice este token en el endpoint correspondiente para completar el cambio de
contraseña.
Si usted no solicitó este cambio, ignore este mensaje.
Respuestas
Código HTTP Resultado Descripción
Se generó el token y se envió al correo del
204 No Content Token generado
usuario.
400 Bad Solicitud El userName está vacío, el usuario no existe
Request inválida o no tiene correo registrado.
Reseteo de password
Endpoint
POST /account/reset-password
Descripción
Permite cambiar la contraseña de un usuario utilizando el token de restablecimiento
recibido por correo electrónico.
Este endpoint no debe requerir JWT, ya que forma parte del flujo de recuperación
de acceso.

Request Body
{
"userId": "1",
"token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9",
"password": "123P@$$word!",
"confirmPassword": "123P@$$word!"
}
Campos del body
Campo Tipo de dato Requerido Descripción
Identificador del usuario al
userId string Sí que se le cambiará la
contraseña.
Token de restablecimiento
token string Sí enviado al correo del
usuario.
Nueva contraseña del
password string Sí
usuario.
Confirmación de la nueva
confirmPassword string Sí
contraseña.
Validaciones
● El campo userId es obligatorio.
● El campo token es obligatorio.
● El campo password es obligatorio.
● El campo confirmPassword es obligatorio.
● El usuario debe existir.
● El token debe existir.
● El token debe estar asociado al usuario indicado.
● El token no debe haber sido utilizado anteriormente.
● La contraseña y la confirmación de contraseña deben coincidir.
Si todas las validaciones son correctas, el sistema debe realizar las siguientes
acciones:
● Cambiar la contraseña del usuario.
● Marcar el token como utilizado.

● Activar nuevamente la cuenta del usuario.
Respuestas
Código HTTP Resultado Descripción
Contraseña La contraseña fue actualizada
204 No Content
cambiada correctamente y el usuario quedó activo.
Faltan campos requeridos, el usuario no
400 Bad Solicitud
existe, el token es inválido o las
Request inválida
contraseñas no coinciden.
Reglas adicionales del módulo
● El endpoint de Login debe generar JWT únicamente para usuarios activos.
● El JWT debe incluir el identificador del usuario, nombre de usuario, rol y fecha de
expiración.
● Los usuarios con rol Administrador y Comercio pueden autenticarse en la API.
● Los usuarios inactivos no pueden obtener JWT.
● El endpoint de Confirmar cuenta debe activar usuarios mediante token.
● El endpoint de Get Reset Token debe enviar el token directamente en el cuerpo
del correo, no como enlace.
● El endpoint de Reset Password debe validar el token antes de cambiar la
contraseña.
● Después de restablecer la contraseña correctamente, el usuario debe quedar
activo.
● Los tokens de confirmación y restablecimiento deben poder utilizarse una sola
vez.
● Las respuestas de error deben mantener códigos HTTP consistentes y mensajes
claros.
Módulo: Gestión de Usuarios
Este módulo permite administrar los usuarios registrados en el sistema desde la
Web API.
Desde estos endpoints, el usuario administrador podrá consultar usuarios, crear
usuarios de la aplicación web, crear usuarios de comercio, actualizar datos, activar o

inactivar usuarios y consultar el detalle de un usuario específico.
Seguridad
Todos los endpoints de este módulo requieren autenticación mediante JWT.
En cada solicitud debe enviarse el siguiente encabezado:
Authorization: Bearer {token_jwt}
Acceso restringido:
Solo los usuarios con rol Administrador pueden consumir los endpoints de este
módulo.
Si la solicitud no contiene un token JWT válido, la API debe responder con:
401 Unauthorized
Si el usuario autenticado no tiene rol Administrador, la API debe responder con:
403 Forbidden
Obtener listado de usuarios
Endpoint
GET /api/users
Descripción
Obtiene un listado paginado de los usuarios registrados en el sistema, excluyendo
los usuarios con rol Comercio.
El listado debe estar ordenado desde el usuario más reciente hasta el más antiguo.
Query Params
Valor por
Parámetro Tipo de dato Requerido Descripción
defecto
Número de página que
page int No 1
se desea consultar.

Valor por
| Parámetro  | Tipo de dato  | Requerido  |     |     | Descripción  |
| ---------- | ------------- | ---------- | --- | --- | ------------ |
defecto
Cantidad de registros por
| pageSize  | int  | No  | 20  |     |     |
| --------- | ---- | --- | --- | --- | --- |
página.
Permite filtrar por tipo de
| role  | string  | No  | null  | usuario: administrador,  |     |
| ----- | ------- | --- | ----- | ------------------------ | --- |
cajero o cliente.

Validaciones
● El parámetro page debe ser mayor que cero.
 ● El parámetro pageSize debe ser mayor que cero.
 ● El valor máximo permitido para pageSize debe ser 20.
 ● El parámetro role, si se envía, solo puede tener los valores administrador, cajero o
cliente.
 ● Los usuarios con rol Comercio no deben incluirse en este listado.
Respuestas

| Código HTTP  |     | Resultado  |     | Descripción  |     |
| ------------ | --- | ---------- | --- | ------------ | --- |
Retorna el listado paginado de
| 200 OK  | Listado retornado  |     |     |     |     |
| ------- | ------------------ | --- | --- | --- | --- |
usuarios.
|     | Parámetros  |     | Algún parámetro de consulta tiene  |     |     |
| --- | ----------- | --- | ---------------------------------- | --- | --- |
400 Bad Request
|     | inválidos  |     | un valor incorrecto.  |     |     |
| --- | ---------- | --- | --------------------- | --- | --- |
401 Unauthorized  No autenticado  Token ausente, inválido o expirado.
El usuario autenticado no tiene rol
| 403 Forbidden  | Acceso denegado  |     |     |     |     |
| -------------- | ---------------- | --- | --- | --- | --- |
Administrador.
Respuesta 200 OK
{
 "page": 1,
 "pageSize": 20,
 "totalRecords": 2,
 "totalPages": 1,

"data": [
{
"id": "1",
"userName": "admin",
"identification": "00112345678",
"firstName": "Juan",
"lastName": "Pérez",
"email": "admin@artemis.com",
"role": "Administrador",
"isActive": true
},
{
"id": "2",
"userName": "cliente01",
"identification": "00187654321",
"firstName": "María",
"lastName": "Gómez",
"email": "cliente01@artemis.com",
"role": "Cliente",
"isActive": false
}
]
}
Obtener listado de usuarios con rol Comercio
Endpoint
GET /api/users/commerce
Descripción
Obtiene un listado paginado de los usuarios registrados con rol Comercio.
El listado debe estar ordenado desde el usuario más reciente hasta el más antiguo.
Este endpoint debe utilizarse para consultar únicamente usuarios asociados a
comercios.

Query Params

Valor por
| Parámetro  | Tipo de dato  | Requerido  |     |     | Descripción  |
| ---------- | ------------- | ---------- | --- | --- | ------------ |
defecto
Número de página que
| page  | int  | No  | 1   |     |     |
| ----- | ---- | --- | --- | --- | --- |
se desea consultar.
Cantidad de registros por
| pageSize  | int  | No  | 20  |     |     |
| --------- | ---- | --- | --- | --- | --- |
página.

Nota: Este endpoint no necesita un filtro por rol, ya que siempre debe retornar
únicamente usuarios con rol Comercio.
Validaciones
● El parámetro page debe ser mayor que cero.
 ● El parámetro pageSize debe ser mayor que cero.
 ● El valor máximo permitido para pageSize debe ser 20.
 ● Solo deben retornar usuarios con rol Comercio.
Respuestas

| Código HTTP  | Resultado  |     |     | Descripción  |     |
| ------------ | ---------- | --- | --- | ------------ | --- |
Retorna el listado paginado de
| 200 OK  | Listado retornado  |     |     |     |     |
| ------- | ------------------ | --- | --- | --- | --- |
usuarios con rol Comercio.
|     | Parámetros  |     | Algún parámetro de consulta tiene  |     |     |
| --- | ----------- | --- | ---------------------------------- | --- | --- |
400 Bad Request
|     | inválidos  |     | un valor incorrecto.  |     |     |
| --- | ---------- | --- | --------------------- | --- | --- |
401 Unauthorized  No autenticado  Token ausente, inválido o expirado.
El usuario autenticado no tiene rol
| 403 Forbidden  | Acceso denegado  |     |     |     |     |
| -------------- | ---------------- | --- | --- | --- | --- |
de administrador.
Respuesta 200 OK
{
 "page": 1,
 "pageSize": 20,

"totalRecords": 1,
"totalPages": 1,
"data": [
{
"id": "10",
"userName": "commerce01",
"identification": "10199999999",
"firstName": "Comercio",
"lastName": "Principal",
"email": "commerce01@artemis.com",
"role": "Comercio",
"commerceId": 5,
"commerceName": "Tienda Demo",
"isActive": true
}
]
}
Crear nuevo usuario
Endpoint
POST /api/users
Descripción
Crea un nuevo usuario con rol Administrador, Cajero o Cliente.
Este endpoint no debe utilizarse para crear usuarios con rol Comercio. Los usuarios
de comercio deben crearse desde el endpoint correspondiente.
Request Body
{
"firstName": "María",
"lastName": "Gómez",
"identification": "00187654321",
"email": "cliente01@artemis.com",
"userName": "cliente01",

 "password": "123P@$$word!",
 "confirmPassword": "123P@$$word!",
 "role": "Cliente",
 "initialAmount": 5000.00
}
Campos del body

Tipo de
| Campo  |     | Requerido  |     | Descripción  |
| ------ | --- | ---------- | --- | ------------ |
dato
| firstName       | string  | Sí  | Nombre del usuario.              |     |
| --------------- | ------- | --- | -------------------------------- | --- |
| lastName        | string  | Sí  | Apellido del usuario.            |     |
| identification  | string  | Sí  | Cédula del usuario.              |     |
| email           | string  | Sí  | Correo electrónico del usuario.  |     |
| userName        | string  | Sí  | Nombre de usuario para iniciar   |     |
sesión.
| password  | string  | Sí  | Contraseña inicial del usuario.  |     |
| --------- | ------- | --- | -------------------------------- | --- |
confirmPassword  string  Sí  Confirmación de la contraseña inicial.
| role  | string  | Sí  | Rol del usuario: Administrador,  |     |
| ----- | ------- | --- | -------------------------------- | --- |
Cajero o Cliente.
initialAmount  decimal  No  Monto inicial de la cuenta principal.
Solo aplica para usuarios con rol
Cliente.
Reglas
● Todos los campos son obligatorios, excepto initialAmount.
 ● El campo role solo puede tener los valores Administrador, Cajero o Cliente.
 ● No se puede crear un usuario con rol Comercio desde este endpoint.
 ● El userName debe ser único.
 ● El email debe ser único.
 ● La cédula debe ser única.
 ● La contraseña y la confirmación de contraseña deben coincidir.

● El usuario debe crearse inicialmente inactivo.
● Luego de crear el usuario, el sistema debe generar un token de activación.
● El token de activación debe enviarse por correo electrónico.
● Desde la API, el correo no debe enviar un enlace de activación. Debe enviar el
token directamente en el cuerpo del correo.
Si el usuario creado tiene rol Cliente, el sistema debe crear automáticamente una
cuenta de ahorro principal con las siguientes características:
● Número de cuenta único de 9 dígitos.
● Balance inicial igual al valor enviado en initialAmount.
● Si initialAmount no se envía, el balance inicial debe ser RD$0.00.
● La cuenta debe quedar marcada como cuenta principal.
● La cuenta debe crearse en estado activa.
● El número de cuenta no puede repetirse como número de cuenta ni como número
de préstamo.
Contenido sugerido del correo
Asunto: Token de activación de cuenta
Hola [Nombre del usuario],
Su cuenta ha sido creada correctamente en Artemis Banking.
Utilice el siguiente token para activar su cuenta desde el endpoint correspondiente:
[TOKEN]
Si usted no esperaba la creación de esta cuenta, ignore este mensaje.
Respuestas
Código HTTP Resultado Descripción
201 Created Usuario creado El usuario fue creado correctamente.
400 Bad Request Solicitud inválida Datos faltantes o inválidos.
401 Unauthorized No autenticado Token ausente, inválido o expirado.
El usuario autenticado no tiene rol
403 Forbidden Acceso denegado
de administrador.

Código HTTP Resultado Descripción
El usuario, correo o cédula ya se
409 Conflict Conflicto
encuentra registrado.
Respuesta 201 Created
{
"id": "2",
"userName": "cliente01",
"email": "cliente01@artemis.com",
"role": "Cliente",
"isActive": false
}
Crear nuevo usuario de comercio
Endpoint
POST /api/users/commerce/{commerceId}
Descripción
Crea un nuevo usuario con rol Comercio y lo asocia al comercio indicado mediante el
parámetro commerceId.
El parámetro commerceId representa el identificador del comercio al que será
asociado el usuario.
Cada comercio solo puede tener un usuario asociado.
Route Params
Parámetro Tipo de dato Requerido Descripción
Identificador del comercio al que se
commerceId int Sí
asociará el usuario.

Request Body
{
 "firstName": "Usuario",
 "lastName": "Comercio",
 "identification": "10199999999",
 "email": "commerce01@artemis.com",
 "userName": "commerce01",
 "password": "123P@$$word!",
 "confirmPassword": "123P@$$word!",
 "initialAmount": 0.00
}
Campos del body
| Campo      | Tipo de dato  | Requerido  | Descripción  |
| ---------- | ------------- | ---------- | ------------ |
| firstName  | string        | Sí         |              |
Nombre del usuario de
comercio.
| lastName  | string  | Sí  Apellido del usuario de  |     |
| --------- | ------- | ---------------------------- | --- |
comercio.
| identification  | string  | Sí  Cédula o identificador del  |     |
| --------------- | ------- | ------------------------------- | --- |
usuario.
| email  | string  | Sí  Correo electrónico del  |     |
| ------ | ------- | --------------------------- | --- |
usuario.
| userName  | string  | Sí  Nombre de usuario para  |     |
| --------- | ------- | --------------------------- | --- |
iniciar sesión en la API.
| password  | string  | Sí  |     |
| --------- | ------- | --- | --- |
Contraseña inicial del
usuario.
| confirmPassword  | string  | Sí  Confirmación de la  |     |
| ---------------- | ------- | ----------------------- | --- |
contraseña inicial.
| initialAmount  | decimal  | Sí  Balance inicial de la cuenta  |     |
| -------------- | -------- | --------------------------------- | --- |
de ahorro principal asociada
al usuario de comercio.

Reglas
● Todos los campos son obligatorios.
● El comercio indicado en commerceId debe existir.
● El comercio no debe tener otro usuario asociado.
● El usuario debe crearse automáticamente con rol Comercio.
● No se debe recibir el rol en el body.
● El userName debe ser único.
● El email debe ser único.
● La cédula debe ser única.
● La contraseña y la confirmación de contraseña deben coincidir.
● El usuario debe crearse inicialmente inactivo.
● Luego de crear el usuario, el sistema debe generar un token de activación.
● Desde la API, el correo no debe enviar un enlace de activación. Debe enviar el
token directamente en el cuerpo del correo.
Además, el sistema debe generar una cuenta de ahorro principal asociada al usuario
con rol Comercio.
La cuenta debe cumplir las siguientes características:
● Número de cuenta único de 9 dígitos.
● Balance inicial igual al valor enviado en initialAmount.
● Marcada como cuenta principal.
● Estado activa.
● El número de cuenta no puede repetirse como número de cuenta ni como número
de préstamo.
Respuestas
Código HTTP Resultado Descripción
El usuario de comercio fue creado
201 Created Usuario creado
correctamente.
Solicitud
400 Bad Request Datos faltantes o inválidos.
inválida
Token ausente, inválido o
401 Unauthorized No autenticado
expirado.
Acceso El usuario autenticado no tiene rol
403 Forbidden
denegado de administrador.

Código HTTP Resultado Descripción
404 Not Found No encontrado El comercio indicado no existe.
El comercio ya tiene un usuario
409 Conflict Conflicto asociado o el
usuario/correo/cédula ya existe.
Respuesta 201 Created
{
"id": "10",
"userName": "commerce01",
"email": "commerce01@artemis.com",
"role": "Comercio",
"commerceId": 5,
"isActive": false
}
Actualizar usuario
Endpoint
PUT /api/users/{id}
Descripción
Modifica los datos de un usuario existente.
No se permite modificar el tipo de usuario desde este endpoint.
Route Params
Parámetro Tipo de dato Requerido Descripción
Identificador del usuario que se
id string Sí
desea actualizar.

Request Body
{
 "firstName": "María",
 "lastName": "Gómez",
 "identification": "00187654321",
 "email": "maria.gomez@artemis.com",
 "userName": "cliente01",
 "password": "123P@$$word!",
 "confirmPassword": "123P@$$word!",
 "additionalAmount": 12000.00
}

Campos del body
Tipo de
| Campo  |     | Requerido  |     | Descripción  |
| ------ | --- | ---------- | --- | ------------ |
dato
| firstName       | string  | Sí  | Nombre del usuario.                 |     |
| --------------- | ------- | --- | ----------------------------------- | --- |
| lastName        | string  | Sí  | Apellido del usuario.               |     |
| identification  | string  | Sí  | Cédula del usuario.                 |     |
| email           | string  | Sí  | Correo electrónico del usuario.     |     |
| userName        | string  | Sí  | Nombre de usuario.                  |     |
| password        | string  | No  | Nueva contraseña. Solo se modifica  |     |
si se envía este campo.
confirmPassword  string  Solo si se  Confirmación de la nueva
contraseña.
envía
password
additionalAmount  decimal  No  Monto adicional que se sumará a la
cuenta principal si el usuario es
Cliente o Comercio.
Reglas
● El usuario indicado debe existir.
 ● No se puede modificar el rol del usuario.

● El userName no puede estar registrado en otro usuario.
● El email no puede estar registrado en otro usuario.
● La cédula no puede estar registrada en otro usuario.
● Si se envía password, también debe enviarse confirmPassword.
● Si se envía password, ambos campos deben coincidir.
● Si password no se envía o se envía vacío, la contraseña actual no debe
modificarse.
● El campo additionalAmount no puede ser negativo.
● Si el usuario es Cliente o Comercio y additionalAmount es mayor que cero, el
monto debe sumarse al balance actual de su cuenta de ahorro principal.
● Si additionalAmount es RD$0.00 o no se envía, no debe modificarse el balance
de la cuenta principal.
● Si se aplica additionalAmount, debe registrarse una transacción de tipo CRÉDITO
en la cuenta principal.
Respuestas
Código HTTP Resultado Descripción
Usuario El usuario fue actualizado
204 No Content
actualizado correctamente.
Solicitud
400 Bad Request Datos faltantes o inválidos.
inválida
Token ausente, inválido o
401 Unauthorized No autenticado
expirado.
Acceso El usuario autenticado no tiene rol
403 Forbidden
denegado Administrador.
404 Not Found No encontrado El usuario indicado no existe.
El correo, usuario o cédula ya
409 Conflict Conflicto
pertenece a otro usuario.
Cambiar estado de usuario
Endpoint
PATCH /api/users/{id}/status

Descripción
Activa o inactiva un usuario según el valor enviado en el body.
Route Params
|     | Parámetro  | Tipo de dato  | Requerido  |     |     | Descripción  |
| --- | ---------- | ------------- | ---------- | --- | --- | ------------ |
Identificador del usuario al
| id  |     | string  |     | Sí  |     |     |
| --- | --- | ------- | --- | --- | --- | --- |
que se le cambiará el estado.
Request Body
{
 "status": true
}
Campos del body

|     | Campo  | Tipo de dato  | Requerido  |     |     | Descripción  |
| --- | ------ | ------------- | ---------- | --- | --- | ------------ |
Nuevo estado del usuario.
|     | status  | boolean  |     | Sí  | true para activo, false para  |     |
| --- | ------- | -------- | --- | --- | ----------------------------- | --- |
inactivo.
Reglas
● El usuario indicado debe existir.
 ● El administrador autenticado no puede modificar su propio estado.
 ● Si status es true, el usuario debe quedar activo.
 ● Si status es false, el usuario debe quedar inactivo.
 ● Inactivar un usuario debe impedir que pueda iniciar sesión.
 ● Cambiar el estado de un usuario no debe eliminar sus productos financieros ni su
historial.
Respuestas
|     | Código HTTP  | Resultado  |                                        |     | Descripción  |     |
| --- | ------------ | ---------- | -------------------------------------- | --- | ------------ | --- |
|     |              | Estado     | El estado del usuario fue actualizado  |     |              |     |
204 No Content
|     |     | cambiado  | correctamente.  |     |     |     |
| --- | --- | --------- | --------------- | --- | --- | --- |

| Código HTTP  |     | Resultado  |     | Descripción  |
| ------------ | --- | ---------- | --- | ------------ |
Solicitud
| 400 Bad Request  |     |     | Body inválido o campo status faltante.  |     |
| ---------------- | --- | --- | --------------------------------------- | --- |
inválida
401 Unauthorized  No autenticado  Token ausente, inválido o expirado.
|     | Acceso  |     | Usuario sin rol Administrador o intento  |     |
| --- | ------- | --- | ---------------------------------------- | --- |
403 Forbidden
|     | denegado  |     | de auto-modificación.  |     |
| --- | --------- | --- | ---------------------- | --- |
404 Not Found  No encontrado  El usuario indicado no existe.
Obtener detalle de usuario
Endpoint
GET /api/users/{id}
Descripción
Recupera la información detallada de un usuario específico.
Route Params

| Parámetro  | Tipo de dato  |     | Requerido  | Descripción  |
| ---------- | ------------- | --- | ---------- | ------------ |
Identificador del usuario
| id  | string  |     | Sí  |     |
| --- | ------- | --- | --- | --- |
que se desea consultar.
Respuestas

| Código HTTP  |     | Resultado  |     | Descripción  |
| ------------ | --- | ---------- | --- | ------------ |
Retorna la información detallada
| 200 OK  | Detalle retornado  |     |     |     |
| ------- | ------------------ | --- | --- | --- |
del usuario.
Token ausente, inválido o
| 401 Unauthorized  | No autenticado  |     |     |     |
| ----------------- | --------------- | --- | --- | --- |
expirado.
El usuario autenticado no tiene rol
| 403 Forbidden  | Acceso denegado  |     |     |     |
| -------------- | ---------------- | --- | --- | --- |
Administrador.

Código HTTP Resultado Descripción
404 Not Found No encontrado El usuario indicado no existe.
Respuesta 200 OK
{
"id": "2",
"userName": "cliente01",
"identification": "00187654321",
"firstName": "María",
"lastName": "Gómez",
"email": "cliente01@artemis.com",
"role": "Cliente",
"isActive": true,
"createdAt": "2026-07-01T10:30:00",
"mainAccount": {
"accountNumber": "123456789",
"balance": 17000.00,
"isPrincipal": true,
"status": "Activa"
}
}
Reglas adicionales del módulo
● Todos los endpoints de este módulo requieren JWT.
● Solo los usuarios con rol Administrador pueden consumir estos endpoints.
● El endpoint GET /api/users debe excluir usuarios con rol Comercio.
● El endpoint GET /api/users/commerce debe retornar únicamente usuarios con rol
Comercio.
● Los listados deben estar paginados y ordenados del más reciente al más antiguo.
● El tamaño máximo de página debe ser 20 registros.
● El userName, email y cédula deben ser únicos en todo el sistema.
● Los usuarios creados desde la API deben quedar inicialmente inactivos.
● Los tokens de activación enviados desde la API deben enviarse en el cuerpo del
correo, no como enlace.
● Los usuarios con rol Cliente deben recibir automáticamente una cuenta de ahorro
principal al ser creados.
● Los usuarios con rol Comercio deben asociarse a un comercio existente y recibir

una cuenta de ahorro principal.
● Un comercio solo puede tener un usuario asociado.
● No se puede modificar el rol de un usuario después de creado.
● El administrador autenticado no puede cambiar su propio estado.
● Cambiar el estado de un usuario no debe eliminar su historial ni sus productos
financieros.
Módulo: Gestión de Préstamos
Este módulo permite administrar los préstamos desde la Web API.
Desde estos endpoints, el usuario administrador podrá consultar préstamos, asignar
nuevos préstamos a clientes, visualizar el detalle de un préstamo con su tabla de
amortización y modificar la tasa de interés anual de un préstamo activo.
Seguridad
Todos los endpoints de este módulo requieren autenticación mediante JWT.
En cada solicitud debe enviarse el siguiente encabezado:
Authorization: Bearer {token_jwt}
Acceso restringido:
Solo los usuarios con rol Administrador pueden consumir los endpoints de este
módulo.
Si la solicitud no contiene un token JWT válido, la API debe responder con:
401 Unauthorized
Si el usuario autenticado no tiene rol Administrador, la API debe responder con:
403 Forbidden
Obtener listado de préstamos
Endpoint
GET /api/loan
Descripción
Obtiene un listado paginado de los préstamos registrados en el sistema.

Por defecto, el listado debe mostrar los préstamos activos, ordenados desde el más
reciente hasta el más antiguo.
El endpoint también debe permitir filtrar por estado y buscar préstamos asociados a
un cliente mediante su cédula.
Query Params
Valor por
Parámetro Tipo de dato Requerido Descripción
defecto
Número de página que
page int No 1
se desea consultar.
Cantidad de registros por
pageSize int No 20
página.
Estado de los préstamos
a consultar. Valores
status string No activos
permitidos: activos,
completados, todos.
Cédula del cliente para
identification string No null
buscar sus préstamos.
Reglas
● El parámetro page debe ser mayor que cero.
● El parámetro pageSize debe ser mayor que cero.
● El valor máximo permitido para pageSize debe ser 20.
● El parámetro status solo puede tener los valores activos, completados o todos.
● Si se envía identification, el sistema debe buscar los préstamos asociados al
cliente correspondiente.
● Si se busca por cédula y no se especifica status, deben mostrarse primero los
préstamos activos y luego los completados.
● Dentro de cada grupo, los préstamos deben mostrarse del más reciente al más
antiguo.

Respuestas
Código HTTP Resultado Descripción
Retorna el listado paginado de
200 OK Listado retornado
préstamos.
Algún parámetro de consulta tiene
400 Bad Request Parámetros inválidos
un valor incorrecto.
Token ausente, inválido o
401 Unauthorized No autenticado
expirado.
El usuario autenticado no tiene rol
403 Forbidden Acceso denegado
Administrador.
Respuesta 200 OK
{
"page": 1,
"pageSize": 20,
"totalRecords": 1,
"totalPages": 1,
"data": [
{
"id": "1",
"loanNumber": "987654321",
"clientId": "20",
"clientFullName": "María Gómez",
"capitalAmount": 100000.00,
"totalInstallments": 12,
"paidInstallments": 3,
"pendingAmount": 76250.00,
"annualInterestRate": 12.00,
"termInMonths": 12,
"status": "Activo",
"clientPaymentStatus": "Al día",
"createdAt": "2026-07-01T10:30:00"
}
]

}
Asignar préstamo a cliente
Endpoint
POST /api/loan
Descripción
Crea un nuevo préstamo para un cliente activo, genera automáticamente su tabla de
amortización, acredita el monto aprobado en la cuenta de ahorro principal del
cliente y registra la transacción correspondiente.
Un cliente solo puede tener un préstamo activo a la vez.
Request Body
{
"clientId": "20",
"capitalAmount": 100000.00,
"termInMonths": 12,
"annualInterestRate": 12.00,
"confirmHighRisk": false
}
Campos del body
Campo Tipo de Requerido Descripción
dato
clientId string Sí Identificador del cliente al que se
asignará el préstamo.
capitalAmount decimal Sí Monto de capital aprobado para
el préstamo.
termInMonths int Sí Plazo del préstamo expresado en
meses.
annualInterestR decimal Sí Tasa de interés anual aplicada al
ate préstamo.

Campo Tipo de Requerido Descripción
dato
confirmHighRisk boolean No Indica si el administrador
confirma la asignación aunque el
cliente sea de alto riesgo.
Reglas
● Todos los campos son obligatorios, excepto confirmHighRisk.
● El cliente debe existir.
● El cliente debe estar activo.
● El cliente no debe tener un préstamo activo actualmente.
● El cliente debe tener una cuenta de ahorro principal activa.
● El monto del préstamo debe ser mayor que cero.
● La tasa de interés anual no puede ser negativa.
● El plazo debe ser uno de los valores permitidos.
● Antes de crear el préstamo, el sistema debe validar si el cliente es o se convierte
en cliente de alto riesgo.
El campo termInMonths solo puede recibir los siguientes valores:
● 6
● 12
● 18
● 24
● 30
● 36
● 42
● 48
● 54
● 60
Validación de cliente de alto riesgo
El sistema debe calcular la deuda promedio de los clientes activos.
La deuda promedio debe tomar en cuenta préstamos activos y deudas de tarjetas
de crédito activas.
El sistema debe considerar al cliente como de alto riesgo si ocurre cualquiera de
estos casos:
● La deuda actual del cliente supera la deuda promedio del sistema.

● La deuda proyectada del cliente, incluyendo el nuevo préstamo, supera la deuda
promedio del sistema.
La deuda proyectada debe calcularse sumando:
Deuda actual del cliente + Total a pagar del nuevo préstamo
El total a pagar del nuevo préstamo corresponde a la suma de todas las cuotas
generadas en la tabla de amortización.
Si el cliente es o se convierte en cliente de alto riesgo y el campo confirmHighRisk
no fue enviado en true, la API debe responder con:
409 Conflict
Esta respuesta debe permitir que el consumidor de la API conozca la razón del
conflicto y, si desea continuar, vuelva a enviar la solicitud con confirmHighRisk en
true.
Respuesta 409 Conflict
{
"message": "Asignar este préstamo convertirá al cliente en un cliente de alto riesgo,
ya que su deuda superará el umbral promedio del sistema.",
"riskType": "ProjectedHighRisk",
"currentDebt": 25000.00,
"projectedDebt": 132500.00,
"averageDebt": 80000.00
}
Si el administrador envía confirmHighRisk en true, el sistema debe permitir la
creación del préstamo aunque el cliente sea considerado de alto riesgo.
Procesamiento del préstamo
Si todas las validaciones son correctas, el sistema debe:
● Crear el préstamo en estado Activo.
● Generar un número de préstamo único de 9 dígitos.
● Generar automáticamente la tabla de amortización.
● Acreditar el monto aprobado a la cuenta de ahorro principal del cliente.
● Registrar una transacción de tipo CRÉDITO en la cuenta principal del cliente.
● Asociar el préstamo al usuario administrador autenticado que realizó la
asignación.
● Enviar un correo electrónico al cliente notificando la aprobación del préstamo.

El número de préstamo debe cumplir las siguientes reglas:
● Debe tener exactamente 9 dígitos.
● Debe ser único en el sistema.
● No debe repetirse como número de préstamo.
● No debe repetirse como número de cuenta de ahorro.
● Debe almacenarse como texto para evitar pérdida de ceros iniciales.
Respuestas
Código HTTP Resultado Descripción
201 Created Préstamo El préstamo fue creado y la tabla de
creado amortización fue generada.
400 Bad Solicitud Datos incompletos, inválidos o cliente ya
Request inválida tiene un préstamo activo.
401 No autenticado Token ausente, inválido o expirado.
Unauthorized
403 Forbidden Acceso El usuario autenticado no tiene rol de
denegado administrador.
404 Not Found No encontrado El cliente indicado no existe.
409 Conflict Alto riesgo El cliente es o se convierte en cliente de alto
riesgo y no se confirmó la asignación.
Respuesta 201 Created
{
"id": "1",
"loanNumber": "987654321",
"clientId": "20",
"clientFullName": "María Gómez",
"capitalAmount": 100000.00,
"termInMonths": 12,
"annualInterestRate": 12.00,
"monthlyInstallment": 8884.88,
"totalAmountToPay": 106618.56,
"status": "Activo",

 "createdAt": "2026-07-01T10:30:00"
}

Obtener detalle de préstamo y tabla de amortización
Endpoint
GET /api/loan/{id}
Descripción
Obtiene el detalle de un préstamo específico y su tabla de amortización.
Route Params

| Parámetro  | Tipo de  | Requerido  |     | Descripción  |
| ---------- | -------- | ---------- | --- | ------------ |
dato
| id  | string  | Sí  | Identificador del préstamo que se desea  |     |
| --- | ------- | --- | ---------------------------------------- | --- |
consultar.
Respuestas
| Código HTTP  | Resultado  |     |     | Descripción  |
| ------------ | ---------- | --- | --- | ------------ |
200 OK  Detalle  Retorna la información del préstamo y su tabla
|                | retornado    | de amortización.                     |     |     |
| -------------- | ------------ | ------------------------------------ | --- | --- |
| 401            | No           | Token ausente, inválido o expirado.  |     |     |
| Unauthorized   | autenticado  |                                      |     |     |
| 403 Forbidden  | Acceso       | El usuario autenticado no tiene rol  |     |     |
|                | denegado     | Administrador.                       |     |     |
| 404 Not        | No           | El préstamo indicado no existe.      |     |     |
| Found          | encontrado   |                                      |     |     |
Respuesta 200 OK
{
 "id": "1",
 "loanNumber": "987654321",

"clientId": "20",
"clientFullName": "María Gómez",
"capitalAmount": 100000.00,
"annualInterestRate": 12.00,
"termInMonths": 12,
"monthlyInstallment": 8884.88,
"pendingAmount": 76250.00,
"status": "Activo",
"clientPaymentStatus": "Al día",
"createdAt": "2026-07-01T10:30:00",
"amortization": [
{
"installmentNumber": 1,
"dueDate": "2026-08-01",
"installmentAmount": 8884.88,
"interestAmount": 1000.00,
"capitalAmount": 7884.88,
"pendingInstallmentAmount": 0.00,
"paymentStatus": "Pagada",
"isLate": false
},
{
"installmentNumber": 2,
"dueDate": "2026-09-01",
"installmentAmount": 8884.88,
"interestAmount": 921.15,
"capitalAmount": 7963.73,
"pendingInstallmentAmount": 8884.88,
"paymentStatus": "Pendiente",
"isLate": false
}
]
}

Editar tasa de interés de préstamo
Endpoint
PATCH /api/loan/{id}/rate
Descripción
Permite modificar la tasa de interés anual de un préstamo activo.
Al actualizar la tasa, el sistema debe recalcular únicamente las cuotas futuras
pendientes. Las cuotas pagadas, parcialmente pagadas, vencidas o con fecha de
vencimiento igual o anterior a la fecha actual no deben modificarse.
Route Params
Parámetro Tipo de Requerido Descripción
dato
id string Sí Identificador del préstamo que se desea
modificar.
Request Body
{
"annualInterestRate": 10.50
}
Campos del body
Campo Tipo de Requerido Descripción
dato
annualInterest decimal Sí Nueva tasa de interés anual que será
Rate aplicada al préstamo.
Reglas
● El préstamo debe existir.
● El préstamo debe estar activo.
● La tasa de interés anual es obligatoria.
● La tasa de interés anual no puede ser negativa.
● Debe existir al menos una cuota futura pendiente para poder recalcular.
● Solo se deben recalcular cuotas futuras pendientes.

 ● Las cuotas pagadas no deben modificarse.
 ● Las cuotas vencidas no deben modificarse.
 ● Las cuotas parcialmente pagadas no deben modificarse.
 ● Las cuotas con fecha de vencimiento igual o anterior a la fecha actual no deben
modificarse.
 ● Después de actualizar la tasa, el sistema debe enviar un correo electrónico al
cliente notificando el cambio.
Respuestas
| Código HTTP  | Resultado    |                                               | Descripción  |
| ------------ | ------------ | --------------------------------------------- | ------------ |
| 204 No       | Tasa         | La tasa fue actualizada y las cuotas futuras  |              |
| Content      | actualizada  | fueron recalculadas.                          |              |
400 Bad  Solicitud  Tasa no proporcionada, tasa inválida o no existen
| Request       | inválida     | cuotas futuras pendientes.              |     |
| ------------- | ------------ | --------------------------------------- | --- |
| 401           | No           | Token ausente, inválido o expirado.     |     |
| Unauthorized  | autenticado  |                                         |     |
| 403           | Acceso       | El usuario autenticado no tiene rol de  |     |
| Forbidden     | denegado     | administrador.                          |     |
| 404 Not       | No           | El préstamo indicado no existe.         |     |
| Found         | encontrado   |                                         |     |
Reglas adicionales del módulo
● Todos los endpoints de este módulo requieren JWT.
 ● Solo los usuarios con rol Administrador pueden consumir estos endpoints.
 ● El listado debe estar paginado y ordenado del más reciente al más antiguo.
 ● Por defecto, el listado debe mostrar préstamos activos.
 ● El endpoint debe permitir filtrar por estado y buscar por cédula del cliente.
 ● Un cliente solo puede tener un préstamo activo a la vez.
 ● Solo se pueden asignar préstamos a clientes activos.
 ● El préstamo debe crearse en estado Activo.
 ● El número de préstamo debe tener 9 dígitos y ser único en el sistema.
 ● El número de préstamo no puede repetirse como número de cuenta de ahorro.
 ● Al crear un préstamo, el monto aprobado debe acreditarse a la cuenta principal
del cliente.
 ● El desembolso del préstamo debe registrarse como una transacción de tipo
CRÉDITO.

● La tabla de amortización debe generarse automáticamente al crear el préstamo.
● La API debe responder 409 Conflict cuando el cliente sea o se convierta en
cliente de alto riesgo y no se haya confirmado la asignación.
● Al modificar la tasa de interés, solo deben recalcularse cuotas futuras pendientes.
● El sistema debe enviar notificaciones por correo cuando se cree un préstamo o se
modifique su tasa de interés.
Módulo: Gestión de Tarjetas de Crédito
Este módulo permite administrar las tarjetas de crédito desde la Web API.
Desde estos endpoints, el usuario administrador podrá consultar tarjetas, asignar
nuevas tarjetas a clientes activos, visualizar los consumos de una tarjeta, modificar
el límite de crédito y cancelar tarjetas que no tengan deuda pendiente.
Seguridad
Todos los endpoints de este módulo requieren autenticación mediante JWT.
En cada solicitud debe enviarse el siguiente encabezado:
Authorization: Bearer {token_jwt}
Acceso restringido:
Solo los usuarios con rol Administrador pueden consumir los endpoints de este
módulo.
Si la solicitud no contiene un token JWT válido, la API debe responder con:
401 Unauthorized
Si el usuario autenticado no tiene rol Administrador, la API debe responder con:
403 Forbidden
Obtener tarjetas de crédito
Endpoint
GET /api/credit-card
Descripción
Retorna un listado paginado de las tarjetas de crédito registradas en el sistema.

Por defecto, el listado debe mostrar las tarjetas activas, ordenadas desde la más
reciente hasta la más antigua.
El endpoint debe permitir filtrar por cédula del cliente y por estado de la tarjeta.
Query Params
Parámetro Tipo de Requerido Valor por Descripción
dato defecto
page int No 1 Número de página que se desea
consultar.
pageSize int No 20 Cantidad de registros por página.
status string No activa Estado de las tarjetas a consultar.
Valores permitidos: activa, cancelada,
todas.
identificati string No null Cédula del cliente para buscar sus
on tarjetas.
Reglas
● El parámetro page debe ser mayor que cero.
● El parámetro pageSize debe ser mayor que cero.
● El valor máximo permitido para pageSize debe ser 20.
● El parámetro status solo puede tener los valores activa, cancelada o todas.
● Si se envía identification, el sistema debe buscar las tarjetas asociadas al cliente
correspondiente.
● Si se busca por cédula y no se especifica status, deben mostrarse primero las
tarjetas activas y luego las canceladas.
● Dentro de cada grupo, las tarjetas deben mostrarse desde la más reciente hasta
la más antigua.
● En los listados no debe mostrarse el número completo de la tarjeta, solo los
últimos cuatro dígitos.

Respuestas
Código HTTP Resultado Descripción
200 OK Listado retornado Retorna el listado paginado de tarjetas de
crédito.
400 Bad Parámetros Algún parámetro de consulta tiene un valor
Request inválidos incorrecto.
401 No autenticado Token ausente, inválido o expirado.
Unauthorized
403 Forbidden Acceso denegado El usuario autenticado no tiene rol de
administrador.
Respuesta 200 OK
{
"page": 1,
"pageSize": 20,
"totalRecords": 1,
"totalPages": 1,
"data": [
{
"id": "1",
"maskedCardNumber": "************1234",
"lastFourDigits": "1234",
"clientId": "20",
"clientFullName": "María Gómez",
"creditLimit": 50000.00,
"availableCredit": 35000.00,
"currentDebt": 15000.00,
"expirationDate": "03/29",
"status": "Activa",
"createdAt": "2026-07-01T10:30:00"
}
]
}

Asignar tarjeta de crédito
Endpoint
POST /api/credit-card
Descripción
Asigna una nueva tarjeta de crédito a un cliente activo.
Al crear la tarjeta, el sistema debe generar automáticamente el número de tarjeta,
la fecha de expiración y el CVC. La tarjeta debe quedar en estado Activa y con
deuda inicial en RD$0.00.
Request Body
{
"clientId": "20",
"creditLimit": 50000.00
}
Campos del body
Campo Tipo de Requerido Descripción
dato
clientId string Sí Identificador del cliente al que se asignará
la tarjeta.
creditLimit decimal Sí Límite de crédito aprobado para la tarjeta.
Reglas
● Todos los campos son obligatorios.
● El cliente debe existir.
● El cliente debe estar activo.
● El límite de crédito debe ser mayor que cero.
● El número de tarjeta debe generarse automáticamente con 16 dígitos.
● El número de tarjeta debe ser único en el sistema.
● La fecha de expiración debe calcularse sumando tres años a la fecha actual.
● La fecha de expiración debe almacenarse en formato MM/AA.
● El CVC debe generarse automáticamente con 3 dígitos.
● El CVC no debe almacenarse en texto plano.
● El CVC debe almacenarse como hash utilizando SHA-256.

● La tarjeta debe crearse con estado Activa.
● La deuda inicial de la tarjeta debe ser RD$0.00.
● El usuario administrador autenticado debe quedar registrado como responsable
de la asignación.
● Después de crear la tarjeta, el sistema debe enviar un correo electrónico al cliente
notificando la asignación.
Respuestas
Código HTTP Resultado Descripción
201 Created Tarjeta creada La tarjeta fue asignada correctamente al
cliente.
400 Bad Solicitud Datos faltantes, inválidos o límite menor o
Request inválida igual a cero.
401 No autenticado Token ausente, inválido o expirado.
Unauthorized
403 Forbidden Acceso El usuario autenticado no tiene rol de
denegado administrador.
404 Not Found No encontrado El cliente indicado no existe.
409 Conflict Conflicto No fue posible generar un número de tarjeta
único.
Respuesta 201 Created
{
"id": "1",
"maskedCardNumber": "************1234",
"lastFourDigits": "1234",
"clientId": "20",
"clientFullName": "María Gómez",
"creditLimit": 50000.00,
"availableCredit": 50000.00,
"currentDebt": 0.00,
"expirationDate": "03/29",
"status": "Activa",
"createdAt": "2026-07-01T10:30:00"

}
Nota de seguridad
La API no debe retornar el CVC completo ni el hash del CVC en las respuestas.
El número completo de tarjeta no debe mostrarse en listados generales ni correos
electrónicos. Para identificar la tarjeta, debe utilizarse el número enmascarado o los
últimos cuatro dígitos.
Ver detalles de una tarjeta
Endpoint
GET /api/credit-card/{id}
Descripción
Retorna la información general de una tarjeta de crédito y el listado de consumos
asociados a esa tarjeta.
Los consumos deben mostrarse desde el más reciente hasta el más antiguo.
Route Params
Parámetro Tipo de Requerido Descripción
dato
id string Sí Identificador único de la tarjeta de
crédito.
Reglas
● La tarjeta indicada debe existir.
● El número completo de la tarjeta no debe exponerse en la respuesta.
● Los consumos deben estar ordenados desde el más reciente hasta el más
antiguo.
● Si el consumo corresponde a un avance de efectivo, el campo commerceName
debe mostrar el texto AVANCE.
● Los consumos pueden tener estado APROBADO o RECHAZADO.

Respuestas
Código HTTP Resultado Descripción
200 OK Detalle Retorna la tarjeta y sus consumos.
retornado
401 No autenticado Token ausente, inválido o expirado.
Unauthorized
403 Forbidden Acceso El usuario autenticado no tiene rol de
denegado administrador.
404 Not Found No encontrado La tarjeta indicada no existe.
Respuesta 200 OK
{
"id": "1",
"maskedCardNumber": "************1234",
"lastFourDigits": "1234",
"clientId": "20",
"clientFullName": "María Gómez",
"creditLimit": 50000.00,
"availableCredit": 35000.00,
"currentDebt": 15000.00,
"expirationDate": "03/29",
"status": "Activa",
"consumptions": [
{
"id": "100",
"date": "2026-07-01T15:40:00",
"amount": 2500.00,
"commerceName": "Supermercado Demo",
"status": "APROBADO"
},
{
"id": "101",
"date": "2026-06-28T11:20:00",
"amount": 1062.50,

"commerceName": "AVANCE",
"status": "APROBADO"
}
]
}
Editar límite de una tarjeta
Endpoint
PATCH /api/credit-card/{id}/limit
Descripción
Permite modificar el límite de crédito de una tarjeta activa.
El nuevo límite puede aumentar o disminuir el límite actual, siempre que no sea
inferior a la deuda actual de la tarjeta.
Route Params
Parámetro Tipo de Requerido Descripción
dato
id string Sí Identificador único de la tarjeta de
crédito.
Request Body
{
"creditLimit": 75000.00
}
Campos del body
Campo Tipo de Requerido Descripción
dato
creditLimit decimal Sí Nuevo límite de crédito aprobado para la
tarjeta.

Reglas
● La tarjeta indicada debe existir.
 ● La tarjeta debe estar activa.
 ● El nuevo límite es obligatorio.
 ● El nuevo límite debe ser mayor que cero.
 ● El nuevo límite no puede ser menor que la deuda actual de la tarjeta.
 ● Luego de actualizar el límite, debe recalcularse el crédito disponible.
 ● Después de actualizar el límite, el sistema debe enviar un correo electrónico al
cliente notificando el cambio.
 ● El correo debe identificar la tarjeta utilizando únicamente sus últimos cuatro
dígitos.
Respuestas
| Código HTTP  | Resultado    |                                           | Descripción  |
| ------------ | ------------ | ----------------------------------------- | ------------ |
| 204 No       | Límite       | El límite fue actualizado correctamente.  |              |
| Content      | actualizado  |                                           |              |
400 Bad  Solicitud  Límite faltante, inválido o menor que la deuda
| Request  | inválida        | actual.                              |     |
| -------- | --------------- | ------------------------------------ | --- |
| 401      | No autenticado  | Token ausente, inválido o expirado.  |     |
Unauthorized
403 Forbidden  Acceso  El usuario autenticado no tiene rol de
|     | denegado  | administrador.  |     |
| --- | --------- | --------------- | --- |
404 Not Found  No encontrado  La tarjeta indicada no existe.

Cancelar tarjeta de crédito
Endpoint
PATCH /api/credit-card/{id}/cancel
Descripción
Cancela una tarjeta de crédito activa, siempre que no tenga deuda pendiente.
Una tarjeta cancelada no puede generar nuevos consumos, pagos ni avances de

efectivo, y no debe mostrarse como producto activo del cliente.
Route Params
| Parámetro  | Tipo de  | Requerido  |     | Descripción  |
| ---------- | -------- | ---------- | --- | ------------ |
dato
id  string  Sí  Identificador único de la tarjeta de crédito
que se desea cancelar.
Reglas
● La tarjeta indicada debe existir.
 ● La tarjeta debe estar activa.
 ● La tarjeta no debe tener deuda pendiente.
 ● Si la deuda actual es mayor que RD$0.00, la tarjeta no puede cancelarse.
 ● Al cancelarse, la tarjeta debe cambiar su estado a Cancelada.
 ● La tarjeta cancelada debe mantenerse en el historial del sistema.
 ● La tarjeta cancelada no debe eliminarse físicamente de la base de datos.
 ● Cualquier intento posterior de consumo, avance de efectivo o pago con esa tarjeta
debe ser rechazado.
Respuestas
| Código HTTP  | Resultado  |     |                                          | Descripción  |
| ------------ | ---------- | --- | ---------------------------------------- | ------------ |
| 204 No       | Tarjeta    |     | La tarjeta fue cancelada correctamente.  |              |
| Content      | cancelada  |     |                                          |              |
400 Bad  Solicitud  La tarjeta tiene deuda pendiente o ya se
| Request        | inválida       |     | encuentra cancelada.                 |     |
| -------------- | -------------- | --- | ------------------------------------ | --- |
| 401            | No             |     | Token ausente, inválido o expirado.  |     |
| Unauthorized   | autenticado    |     |                                      |     |
| 403 Forbidden  | Acceso         |     | El usuario autenticado no tiene rol  |     |
|                | denegado       |     | Administrador.                       |     |
| 404 Not        | No encontrado  |     | La tarjeta indicada no existe.       |     |
Found
Respuesta 400 Bad Request por deuda pendiente
{
 "message": "Para cancelar esta tarjeta, el cliente debe saldar la totalidad de la

deuda pendiente."
}
Reglas adicionales del módulo
● Todos los endpoints de este módulo requieren JWT.
● Solo los usuarios con rol Administrador pueden consumir estos endpoints.
● El listado debe estar paginado y ordenado desde la tarjeta más reciente hasta la
más antigua.
● Por defecto, el listado debe mostrar tarjetas activas.
● El endpoint debe permitir filtrar por cédula del cliente y por estado de la tarjeta.
● Solo se pueden asignar tarjetas de crédito a clientes activos.
● El límite de crédito debe ser mayor que cero.
● La deuda inicial de una tarjeta nueva debe ser RD$0.00.
● El número de tarjeta debe tener 16 dígitos y ser único en el sistema.
● El CVC debe tener 3 dígitos y almacenarse como hash SHA-256.
● La fecha de expiración debe generarse automáticamente sumando tres años a la
fecha actual.
● El número completo de tarjeta no debe exponerse en listados, correos ni
respuestas generales.
● El nuevo límite de una tarjeta no puede ser menor que la deuda actual.
● Solo se pueden cancelar tarjetas activas sin deuda pendiente.
● Cancelar una tarjeta no debe eliminar su historial de consumos.
● Una tarjeta cancelada no puede generar consumos, avances de efectivo ni pagos.
● El sistema debe enviar correo al cliente cuando se le asigne una tarjeta.
● El sistema debe enviar correo al cliente cuando se modifique el límite de una
tarjeta.
Módulo: Gestión de Cuentas de Ahorro
Este módulo permite administrar las cuentas de ahorro desde la Web API.
Desde estos endpoints, el usuario administrador podrá consultar cuentas de ahorro,
asignar nuevas cuentas secundarias a clientes, visualizar el historial de
transacciones de una cuenta y cancelar cuentas secundarias cuando corresponda.
Seguridad
Todos los endpoints de este módulo requieren autenticación mediante JWT.
En cada solicitud debe enviarse el siguiente encabezado:
Authorization: Bearer {token_jwt}

Acceso restringido:
Solo los usuarios con rol Administrador pueden consumir los endpoints de este
módulo.
Si la solicitud no contiene un token JWT válido, la API debe responder con:
401 Unauthorized
Si el usuario autenticado no tiene rol Administrador, la API debe responder con:
403 Forbidden
Obtener listado de cuentas de ahorro
Endpoint
GET /api/savings-account
Descripción
Obtiene un listado paginado de las cuentas de ahorro registradas en el sistema.
Por defecto, el listado debe mostrar las cuentas activas, tanto principales como
secundarias, ordenadas desde la más reciente hasta la más antigua.
El endpoint debe permitir filtrar por cédula del cliente, estado de la cuenta y tipo de
cuenta.
Query Params
| Parámetro  | Tipo  | Requerido  | Valor    |                                | Descripción  |
| ---------- | ----- | ---------- | -------- | ------------------------------ | ------------ |
|            | de    |            | por      |                                |              |
|            | dato  |            | defecto  |                                |              |
| page       | int   | No         | 1        | Número de página que se desea  |              |
consultar.
| pageSize  | int  | No  | 20  | Cantidad de registros por página.  |     |
| --------- | ---- | --- | --- | ---------------------------------- | --- |
identificati string  No  null  Cédula del cliente para buscar sus
| on  |     |     |     | cuentas de ahorro.  |     |
| --- | --- | --- | --- | ------------------- | --- |
status  string  No  activa  Estado de las cuentas a consultar.
Valores permitidos: activa,

cancelada, todas.
type string No todas Tipo de cuenta a consultar. Valores
permitidos: principal, secundaria,
todas.
Reglas
● El parámetro page debe ser mayor que cero.
● El parámetro pageSize debe ser mayor que cero.
● El valor máximo permitido para pageSize debe ser 20.
● El parámetro status solo puede tener los valores activa, cancelada o todas.
● El parámetro type solo puede tener los valores principal, secundaria o todas.
● Si se envía identification, el sistema debe buscar las cuentas asociadas al cliente
correspondiente.
● Si se busca por cédula y no se especifica status, deben mostrarse primero las
cuentas activas y luego las canceladas.
● Dentro de cada grupo, las cuentas deben mostrarse desde la más reciente hasta
la más antigua.
Respuestas
Código HTTP Resultado Descripción
200 OK Listado retornado Retorna el listado paginado de cuentas de
ahorro.
400 Bad Parámetros Algún parámetro de consulta tiene un valor
Request inválidos incorrecto.
401 No autenticado Token ausente, inválido o expirado.
Unauthorized
403 Forbidden Acceso denegado El usuario autenticado no tiene rol de
administrador.
Respuesta 200 OK
{
"page": 1,
"pageSize": 20,
"totalRecords": 1,
"totalPages": 1,

"data": [
{
"id": "1",
"accountNumber": "123456789",
"clientId": "20",
"clientFullName": "María Gómez",
"identification": "00187654321",
"balance": 17500.00,
"type": "Principal",
"status": "Activa",
"createdAt": "2026-07-01T10:30:00"
}
]
}
Asignar cuenta de ahorro secundaria a cliente
Endpoint
POST /api/savings-account
Descripción
Crea una nueva cuenta de ahorro secundaria para un cliente activo.
Este endpoint no debe crear cuentas principales. Las cuentas principales se crean
automáticamente al crear un usuario de tipo Cliente o Comercio, según corresponda.
Request Body
{
"clientId": "20",
"initialBalance": 5000.00
}
Campos del body
Campo Tipo de Requerido Descripción
dato
clientId string Sí Identificador del cliente al que se
asignará la cuenta secundaria.

initialBalance decimal Sí Balance inicial de la cuenta. Puede ser
RD$0.00, pero no puede ser negativo.
Reglas
● Todos los campos son obligatorios.
● El cliente debe existir.
● El cliente debe estar activo.
● El cliente debe tener una cuenta principal activa.
● La cuenta creada debe ser de tipo Secundaria.
● La cuenta debe crearse en estado Activa.
● El balance inicial puede ser RD$0.00.
● El balance inicial no puede ser negativo.
● El número de cuenta debe generarse automáticamente con 9 dígitos.
● El número de cuenta debe ser único en el sistema.
● El número de cuenta no puede repetirse como número de cuenta de ahorro.
● El número de cuenta no puede repetirse como número de préstamo.
● El usuario administrador autenticado debe quedar registrado como responsable
de la asignación.
● Si el balance inicial es mayor que RD$0.00, debe registrarse una transacción
inicial de tipo CRÉDITO.
Respuestas
Código HTTP Resultado Descripción
201 Created Cuenta creada La cuenta de ahorro secundaria fue asignada
correctamente.
400 Bad Solicitud Datos inválidos, campos faltantes o balance
Request inválida negativo.
401 No Token ausente, inválido o expirado.
Unauthorized autenticado
403 Forbidden Acceso El usuario autenticado no tiene rol de
denegado administrador.
404 Not Found No encontrado El cliente indicado no existe.
409 Conflict Conflicto No fue posible generar un número de cuenta
único.

Respuesta 201 Created
{
"id": "5",
"accountNumber": "987654321",
"clientId": "20",
"clientFullName": "María Gómez",
"balance": 5000.00,
"type": "Secundaria",
"status": "Activa",
"createdAt": "2026-07-01T11:15:00"
}
Obtener detalles de transacciones por cuenta
Endpoint
GET /api/savings-account/{accountNumber}/transactions
Descripción
Retorna el historial de transacciones registradas para una cuenta de ahorro
específica.
Las transacciones deben mostrarse desde la más reciente hasta la más antigua.
Route Params
Parámetro Tipo de Requerido Descripción
dato
accountNumber string Sí Número identificador de 9 dígitos de la
cuenta de ahorro.
Query Params
Parámetro Tipo Requerido Valor por Descripción
de defecto
dato
page int No 1 Número de página que se desea
consultar.

pageSize int No 20 Cantidad de transacciones por
página.
Reglas
● La cuenta debe existir.
● El parámetro page debe ser mayor que cero.
● El parámetro pageSize debe ser mayor que cero.
● El valor máximo permitido para pageSize debe ser 20.
● Las transacciones deben mostrarse desde la más reciente hasta la más antigua.
● Las transacciones deben indicar si fueron DÉBITO o CRÉDITO.
● Las transacciones deben indicar si fueron APROBADA o RECHAZADA.
Respuestas
Código HTTP Resultado Descripción
200 OK Detalle retornado Retorna la cuenta y su historial de
transacciones.
400 Bad Parámetros Algún parámetro de consulta tiene un valor
Request inválidos incorrecto.
401 No autenticado Token ausente, inválido o expirado.
Unauthorized
403 Forbidden Acceso denegado El usuario autenticado no tiene rol de
administrador.
404 Not Found No encontrado La cuenta indicada no existe.
Respuesta 200 OK
{
"accountNumber": "123456789",
"clientFullName": "María Gómez",
"balance": 17500.00,
"type": "Principal",
"status": "Activa",
"transactions": {
"page": 1,
"pageSize": 20,

"totalRecords": 2,
"totalPages": 1,
"data": [
{
"id": "100",
"date": "2026-07-01T12:00:00",
"amount": 5000.00,
"transactionType": "CRÉDITO",
"origin": "DEPÓSITO",
"beneficiary": "123456789",
"status": "APROBADA"
},
{
"id": "101",
"date": "2026-07-01T14:30:00",
"amount": 1500.00,
"transactionType": "DÉBITO",
"origin": "123456789",
"beneficiary": "987654321",
"status": "APROBADA"
}
]
}
}
Cancelar cuenta de ahorro secundaria
Endpoint
PATCH /api/savings-account/{accountNumber}/cancel
Descripción
Cancela una cuenta de ahorro secundaria activa.
Las cuentas principales no pueden cancelarse. Si la cuenta secundaria tiene balance
disponible, el sistema debe transferir automáticamente ese balance a la cuenta
principal activa del mismo cliente antes de cancelarla.

Route Params
| Parámetro  | Tipo  Requerido  |     | Descripción  |
| ---------- | ---------------- | --- | ------------ |
de
dato
accountNumber  string  Sí  Número identificador de 9 dígitos de la
cuenta que se desea cancelar.
Reglas
● La cuenta debe existir.
 ● La cuenta debe estar activa.
 ● La cuenta debe ser secundaria.
 ● Las cuentas principales no pueden cancelarse.
 ● El cliente debe tener una cuenta principal activa para recibir los fondos.
 ● Si la cuenta secundaria tiene balance mayor que RD$0.00, ese balance debe
transferirse a la cuenta principal del cliente.
 ● Luego de transferir el balance, la cuenta secundaria debe quedar con balance
RD$0.00.
 ● La cuenta secundaria debe cambiar su estado a Cancelada.
 ● La cuenta cancelada no debe eliminarse físicamente de la base de datos.
 ● La cuenta cancelada no debe aparecer como producto activo del cliente.
 ● Cualquier intento posterior de transacción o pago con esa cuenta debe ser
rechazado.
Si la cuenta tiene balance disponible, el sistema debe registrar dos transacciones:
● Una transacción de tipo DÉBITO en la cuenta secundaria que será cancelada.
 ● Una transacción de tipo CRÉDITO en la cuenta principal que recibirá los fondos.
Respuestas
| Código HTTP  | Resultado  |                                     | Descripción  |
| ------------ | ---------- | ----------------------------------- | ------------ |
| 204 No       | Cuenta     | La cuenta secundaria fue cancelada  |              |
| Content      | cancelada  | correctamente.                      |              |
400 Bad  Solicitud  La cuenta es principal, ya está cancelada o no
| Request       | inválida     | puede cancelarse.                    |     |
| ------------- | ------------ | ------------------------------------ | --- |
| 401           | No           | Token ausente, inválido o expirado.  |     |
| Unauthorized  | autenticado  |                                      |     |

403 Forbidden Acceso El usuario autenticado no tiene rol de
denegado administrador.
404 Not No La cuenta indicada no existe.
Found encontrado
Respuesta 400 Bad Request para cuenta principal
{
"message": "Las cuentas principales no pueden ser canceladas."
}
Reglas adicionales del módulo
● Todos los endpoints de este módulo requieren JWT.
● Solo los usuarios con rol Administrador pueden consumir estos endpoints.
● El listado debe estar paginado y ordenado desde la cuenta más reciente hasta la
más antigua.
● Por defecto, el listado debe mostrar cuentas activas.
● El endpoint debe permitir filtrar por cédula, estado y tipo de cuenta.
● Desde este módulo solo se deben crear cuentas de ahorro secundarias.
● Solo se pueden asignar cuentas de ahorro secundarias a clientes activos.
● El cliente debe tener una cuenta principal activa antes de recibir una cuenta
secundaria.
● El número de cuenta debe tener 9 dígitos y ser único en el sistema.
● El número de cuenta no puede repetirse como número de préstamo.
● El balance inicial puede ser RD$0.00, pero no puede ser negativo.
● Todo balance inicial mayor que cero debe registrarse como una transacción de
tipo CRÉDITO.
● Las cuentas principales no pueden cancelarse.
● Solo pueden cancelarse cuentas secundarias activas.
● Si una cuenta secundaria tiene balance al cancelarse, ese balance debe
transferirse a la cuenta principal del mismo cliente.
● Una cuenta cancelada no puede generar nuevas transacciones ni pagos.
● Cancelar una cuenta no debe eliminar su historial de transacciones.
Módulo: Gestión de Comercios
Este módulo permite administrar los comercios registrados en el sistema desde la
Web API.
Desde estos endpoints, el usuario administrador podrá consultar comercios, obtener

el detalle de un comercio específico, crear nuevos comercios, actualizar sus datos y
activar o desactivar comercios existentes.
Los comercios serán utilizados por el procesador de pagos Hermes Pay y podrán
tener un usuario asociado con rol Comercio, creado desde el módulo de Gestión de
Usuarios.
Seguridad
Todos los endpoints de este módulo requieren autenticación mediante JWT.
En cada solicitud debe enviarse el siguiente encabezado:
Authorization: Bearer {token_jwt}
Acceso restringido:
Solo los usuarios con rol Administrador pueden consumir los endpoints de este
módulo.
Si la solicitud no contiene un token JWT válido, la API debe responder con:
401 Unauthorized
Si el usuario autenticado no tiene rol Administrador, la API debe responder con:
403 Forbidden
Obtener todos los comercios
Endpoint
GET /api/commerce
Descripción
Devuelve un listado paginado de comercios registrados en el sistema.
Por defecto, el listado debe mostrar los comercios activos, ordenados desde el más
reciente hasta el más antiguo.
Para mantener consistencia con los demás endpoints de listado, si no se envían
parámetros de paginación, el sistema debe utilizar page = 1 y pageSize = 20.

Query Params
| Parámetro  | Tipo  Requerido  | Valor    |                                | Descripción  |
| ---------- | ---------------- | -------- | ------------------------------ | ------------ |
|            | de               | por      |                                |              |
|            | dato             | defecto  |                                |              |
| page       | int  No          | 1        | Número de página que se desea  |              |
consultar.
| pageSize  | int  No  | 20  | Cantidad de registros por página.  |     |
| --------- | -------- | --- | ---------------------------------- | --- |
status  strin No  activo  Estado de los comercios a consultar.
|     | g   |     | Valores permitidos: activo, inactivo,  |     |
| --- | --- | --- | -------------------------------------- | --- |
todos.
Reglas
● El parámetro page debe ser mayor que cero.
 ● El parámetro pageSize debe ser mayor que cero.
 ● El valor máximo permitido para pageSize debe ser 20.
 ● El parámetro status solo puede tener los valores activo, inactivo o todos.
 ● Si no se envía status, deben retornar únicamente comercios activos.
 ● Los comercios deben ordenarse desde el más reciente hasta el más antiguo.
Respuestas
| Código HTTP  | Resultado  |     | Descripción  |     |
| ------------ | ---------- | --- | ------------ | --- |
200 OK  Listado retornado  Retorna el listado paginado de comercios.
400 Bad  Parámetros  Algún parámetro de consulta tiene un valor
| Request  | inválidos       | incorrecto.                          |     |     |
| -------- | --------------- | ------------------------------------ | --- | --- |
| 401      | No autenticado  | Token ausente, inválido o expirado.  |     |     |
Unauthorized
403 Forbidden  Acceso denegado  El usuario autenticado no tiene rol
Administrador.
Respuesta 200 OK
{
 "page": 1,
 "pageSize": 20,

"totalRecords": 1,
"totalPages": 1,
"data": [
{
"id": 5,
"name": "Tienda Demo",
"description": "Comercio de prueba para pagos Hermes Pay",
"email": "contacto@tiendademo.com",
"phoneNumber": "8095551234",
"rnc": "101999999",
"isActive": true,
"hasAssociatedUser": true,
"createdAt": "2026-07-01T10:30:00"
}
]
}
Obtener comercio por ID
Endpoint
GET /api/commerce/{id}
Descripción
Devuelve la información detallada de un comercio específico según su identificador.
Route Params
Parámetro Tipo de Requerido Descripción
dato
id int Sí Identificador del comercio que se desea
consultar.
Respuestas
Código HTTP Resultado Descripción
200 OK Detalle retornado Retorna la información detallada del comercio.

401 No autenticado Token ausente, inválido o expirado.
Unauthorized
403 Forbidden Acceso denegado El usuario autenticado no tiene rol de
administrador.
404 Not Found No encontrado El comercio indicado no existe.
Respuesta 200 OK
{
"id": 5,
"name": "Tienda Demo",
"description": "Comercio de prueba para pagos Hermes Pay",
"email": "contacto@tiendademo.com",
"phoneNumber": "8095551234",
"rnc": "101999999",
"isActive": true,
"createdAt": "2026-07-01T10:30:00",
"associatedUser": {
"id": "10",
"userName": "commerce01",
"email": "commerce01@artemis.com",
"isActive": true
}
}
Crear nuevo comercio
Endpoint
POST /api/commerce
Descripción
Crea un nuevo comercio en el sistema.
Este endpoint solo registra la información del comercio. El usuario con rol Comercio
debe crearse posteriormente desde el endpoint correspondiente del módulo de

Gestión de Usuarios.
Request Body
{
"name": "Tienda Demo",
"description": "Comercio de prueba para pagos Hermes Pay",
"email": "contacto@tiendademo.com",
"phoneNumber": "8095551234",
"rnc": "101999999"
}
Campos del body
Campo Tipo de Requerido Descripción
dato
name string Sí Nombre comercial del comercio.
description string No Descripción general del comercio.
email string Sí Correo electrónico de contacto del
comercio.
phoneNumber string Sí Número telefónico del comercio.
rnc string Sí Identificador fiscal o RNC del
comercio.
Reglas
● El nombre del comercio es obligatorio.
● El correo electrónico es obligatorio.
● El correo electrónico debe tener un formato válido.
● El teléfono es obligatorio.
● El RNC es obligatorio.
● No debe existir otro comercio con el mismo RNC.
● No debe existir otro comercio con el mismo correo electrónico.
● El comercio debe crearse en estado Activo.
● El usuario administrador autenticado debe quedar registrado como responsable
de la creación.

Respuestas
| Código HTTP  | Resultado  |     | Descripción  |
| ------------ | ---------- | --- | ------------ |
201 Created  Comercio  El comercio fue creado correctamente.
creado
| 400 Bad       | Solicitud    | Datos faltantes o inválidos.         |     |
| ------------- | ------------ | ------------------------------------ | --- |
| Request       | inválida     |                                      |     |
| 401           | No           | Token ausente, inválido o expirado.  |     |
| Unauthorized  | autenticado  |                                      |     |
403 Forbidden  Acceso  El usuario autenticado no tiene rol de
|     | denegado  | administrador.  |     |
| --- | --------- | --------------- | --- |
409 Conflict  Conflicto  Ya existe un comercio con el mismo RNC o
correo electrónico.
Respuesta 201 Created
{
 "id": 5,
 "name": "Tienda Demo",
 "description": "Comercio de prueba para pagos Hermes Pay",
 "email": "contacto@tiendademo.com",
 "phoneNumber": "8095551234",
 "rnc": "101999999",
 "isActive": true,
 "createdAt": "2026-07-01T10:30:00"
}

Actualizar comercio existente
Endpoint
PUT /api/commerce/{id}
Descripción
Actualiza los datos de un comercio existente.

Este endpoint no debe modificar el estado del comercio. Para activar o desactivar un
comercio debe utilizarse el endpoint de cambio de estado.
Route Params
| Parámetro  | Tipo de  | Requerido  |     | Descripción  |
| ---------- | -------- | ---------- | --- | ------------ |
dato
| id  | int  | Sí  | Identificador del comercio que se desea  |     |
| --- | ---- | --- | ---------------------------------------- | --- |
actualizar.
Request Body
{
 "name": "Tienda Demo Actualizada",
 "description": "Comercio actualizado para pagos Hermes Pay",
 "email": "contacto.actualizado@tiendademo.com",
 "phoneNumber": "8095555678",
 "rnc": "101999999"
}
Campos del body
| Campo  | Tipo de  | Requerido  |     | Descripción  |
| ------ | -------- | ---------- | --- | ------------ |
dato
| name         | string  | Sí  | Nombre comercial del comercio.      |     |
| ------------ | ------- | --- | ----------------------------------- | --- |
| description  | string  | No  | Descripción general del comercio.   |     |
| email        | string  | Sí  | Correo electrónico de contacto del  |     |
comercio.
| phoneNumber  | string  | Sí  | Número telefónico del comercio.  |     |
| ------------ | ------- | --- | -------------------------------- | --- |
| rnc          | string  | Sí  | Identificador fiscal o RNC del   |     |
comercio.
Reglas
● El comercio indicado debe existir.
 ● El nombre del comercio es obligatorio.
 ● El correo electrónico es obligatorio.
 ● El correo electrónico debe tener un formato válido.

● El teléfono es obligatorio.
● El RNC es obligatorio.
● El RNC no puede pertenecer a otro comercio.
● El correo electrónico no puede pertenecer a otro comercio.
● El estado del comercio no debe modificarse desde este endpoint.
Respuestas
Código HTTP Resultado Descripción
204 No Comercio Los datos del comercio fueron actualizados
Content actualizado correctamente.
400 Bad Solicitud inválida Datos faltantes o inválidos.
Request
401 No autenticado Token ausente, inválido o expirado.
Unauthorized
403 Forbidden Acceso denegado El usuario autenticado no tiene rol de
administrador.
404 Not No encontrado El comercio indicado no existe.
Found
409 Conflict Conflicto El RNC o correo electrónico pertenece a otro
comercio.
Cambiar estado de un comercio
Endpoint
PATCH /api/commerce/{id}/status
Descripción
Activa o desactiva un comercio según el valor enviado en el body.
Cuando un comercio se desactiva, todos los usuarios asociados a ese comercio
deben quedar inactivos.
Si posteriormente el comercio se reactiva, los usuarios asociados deben permanecer

inactivos. Para volver a utilizarlos, deberán completar el proceso de
restablecimiento de contraseña o activación definido por el sistema.
Route Params
Parámetro Tipo de Requerido Descripción
dato
id int Sí Identificador del comercio al que se le
cambiará el estado.
Request Body
{
"status": true
}
Campos del body
Campo Tipo de Requerido Descripción
dato
status boolean Sí Nuevo estado del comercio. true para activo,
false para inactivo.
Reglas
● El comercio indicado debe existir.
● El campo status es obligatorio.
● Si status es false, el comercio debe quedar inactivo.
● Al desactivar un comercio, todos los usuarios asociados a ese comercio deben
quedar inactivos.
● Si status es true, el comercio debe quedar activo.
● Al reactivar un comercio, los usuarios asociados no deben activarse
automáticamente.
● Los usuarios asociados deben realizar el proceso de restablecimiento de
contraseña para volver a quedar activos.
● Un comercio inactivo no debe poder procesar pagos mediante Hermes Pay.
● Cambiar el estado de un comercio no debe eliminar su historial ni sus
transacciones.

Respuestas
| Código HTTP  | Resultado       |                                         | Descripción  |
| ------------ | --------------- | --------------------------------------- | ------------ |
| 204 No       | Estado          | El estado del comercio fue actualizado  |              |
| Content      | cambiado        | correctamente.                          |              |
| 400 Bad      | Solicitud       | Body inválido o campo status faltante.  |              |
| Request      | inválida        |                                         |              |
| 401          | No autenticado  | Token ausente, inválido o expirado.     |              |
Unauthorized
403 Forbidden  Acceso  El usuario autenticado no tiene rol de
|     | denegado  | administrador.  |     |
| --- | --------- | --------------- | --- |
404 Not Found  No encontrado  El comercio indicado no existe.
Reglas adicionales del módulo
● Todos los endpoints de este módulo requieren JWT.
 ● Solo los usuarios con rol Administrador pueden consumir estos endpoints.
 ● Los comercios deben listarse ordenados desde el más reciente hasta el más
antiguo.
 ● Por defecto, el listado debe mostrar comercios activos.
 ● El listado debe estar paginado con page = 1 y pageSize = 20 por defecto.
 ● El tamaño máximo de página debe ser 20 registros.
 ● No debe existir más de un comercio con el mismo RNC.
 ● No debe existir más de un comercio con el mismo correo electrónico.
 ● Crear un comercio no crea automáticamente un usuario de comercio.
 ● El usuario de comercio debe crearse desde el endpoint POST
/api/users/commerce/{commerceId}.
 ● Un comercio solo puede tener un usuario asociado.
 ● Un comercio inactivo no puede procesar pagos mediante Hermes Pay.
 ● Al desactivar un comercio, sus usuarios asociados deben inactivarse.
 ● Al reactivar un comercio, sus usuarios asociados deben permanecer inactivos
hasta completar el proceso de restablecimiento de contraseña.
 ● Cambiar el estado de un comercio no debe eliminar su historial ni sus registros
asociados.

Módulo: Procesador de Pago (Hermes Pay)
Este módulo permite procesar pagos realizados con tarjetas de crédito a favor de
comercios registrados en el sistema.
Hermes Pay permitirá que un comercio reciba pagos en su cuenta de ahorro
principal utilizando tarjetas de crédito emitidas dentro del sistema. También
permitirá consultar las transacciones recibidas por un comercio.
Seguridad
Todos los endpoints de este módulo requieren autenticación mediante JWT.
En cada solicitud debe enviarse el siguiente encabezado:
Authorization: Bearer {token_jwt}
Acceso restringido:
Solo los usuarios con rol Administrador o Comercio pueden consumir estos
endpoints.
Si la solicitud no contiene un token JWT válido, la API debe responder con:
401 Unauthorized
Si el usuario autenticado no tiene rol Administrador ni rol Comercio, la API debe
responder con:
403 Forbidden
Regla de comportamiento según el rol
El comportamiento de los endpoints de Hermes Pay depende del rol del usuario
autenticado.
Rol Comportamiento
autenticado
Administrador Puede consultar y procesar pagos para cualquier comercio, siempre
que envíe el commerceId correspondiente en la URL.
Comercio Solo puede consultar y procesar pagos para el comercio asociado a
su usuario. Si envía un commerceId en la URL, el sistema debe
ignorarlo y usar el comercio asociado al token JWT.

Si el usuario con rol Comercio no tiene un comercio asociado, la API debe responder
con 403 Forbidden.
Si el comercio asociado se encuentra inactivo, la API debe rechazar la operación.
Obtener transacciones de un comercio
Endpoint
GET /pay/get-transactions/{commerceId}
Descripción
Obtiene un listado paginado de las transacciones registradas para un comercio.
Estas transacciones corresponden a los pagos recibidos en la cuenta de ahorro
principal del usuario asociado al comercio.
El comportamiento depende del rol autenticado:
● Si el usuario autenticado tiene rol Comercio, el sistema debe obtener el comercio
desde el token JWT e ignorar el commerceId enviado en la URL.
● Si el usuario autenticado tiene rol Administrador, el sistema debe usar el
commerceId enviado en la URL.
Route Params
Parámetro Tipo de Requerido Descripción
dato
commerceId int Sí Identificador del comercio del cual se desean
consultar las transacciones. Para usuarios con
rol Comercio, este valor debe ignorarse y se
debe usar el comercio asociado al token.
Query Params

| Parámetro  | Tipo de  | Requerido  | Valor por  |                          | Descripción  |
| ---------- | -------- | ---------- | ---------- | ------------------------ | ------------ |
|            | dato     |            | defecto    |                          |              |
| page       | int      | No         | 1          | Número de página que se  |              |
desea consultar.
| pageSize  | int  | No  | 20  | Cantidad de registros por  |     |
| --------- | ---- | --- | --- | -------------------------- | --- |
página.
Reglas
● El usuario debe estar autenticado mediante JWT.
 ● El usuario debe tener rol Administrador o Comercio.
 ● Si el usuario tiene rol Comercio, solo puede consultar transacciones de su propio
comercio.
 ● Si el usuario tiene rol Administrador, debe enviar un commerceId válido.
 ● El comercio debe existir.
 ● El parámetro page debe ser mayor que cero.
 ● El parámetro pageSize debe ser mayor que cero.
 ● El valor máximo permitido para pageSize debe ser 20.
 ● Las transacciones deben mostrarse desde la más reciente hasta la más antigua.
Respuestas
| Código HTTP  | Resultado  |     |     | Descripción  |     |
| ------------ | ---------- | --- | --- | ------------ | --- |
200 OK  Listado  Retorna el listado paginado de transacciones
|     | retornado  |     | del comercio.  |     |     |
| --- | ---------- | --- | -------------- | --- | --- |
400 Bad  Parámetros  Algún parámetro de consulta tiene un valor
| Request  | inválidos       |     | incorrecto.                          |     |     |
| -------- | --------------- | --- | ------------------------------------ | --- | --- |
| 401      | No autenticado  |     | Token ausente, inválido o expirado.  |     |     |
Unauthorized
| 403        | Acceso         |     | El usuario autenticado no tiene permisos para  |     |     |
| ---------- | -------------- | --- | ---------------------------------------------- | --- | --- |
| Forbidden  | denegado       |     | consultar el comercio indicado.                |     |     |
| 404 Not    | No encontrado  |     | El comercio indicado no existe.                |     |     |
Found

Respuesta 200 OK

{
"page": 1,
"pageSize": 20,
"totalRecords": 2,
"totalPages": 1,
"commerceId": 5,
"commerceName": "Tienda Demo",
"data": [
{
"id": "100",
"transactionDate": "2026-07-01T15:40:00",
"amount": 2500.00,
"cardLastFourDigits": "1234",
"status": "APROBADO"
},
{
"id": "101",
"transactionDate": "2026-06-30T11:20:00",
"amount": 850.00,
"cardLastFourDigits": "5678",
"status": "APROBADO"
}
]
}
Procesar pago de un comercio
Endpoint
POST /pay/process-payment/{commerceId}
Descripción
Procesa un pago realizado con tarjeta de crédito a favor de un comercio.
Si la operación es aprobada, el sistema debe registrar el consumo en la tarjeta de
crédito utilizada, aumentar la deuda de la tarjeta y acreditar el monto pagado en la
cuenta de ahorro principal del comercio.

El comportamiento depende del rol autenticado:
● Si el usuario autenticado tiene rol Comercio, el sistema debe obtener el comercio
desde el token JWT e ignorar el commerceId enviado en la URL.
● Si el usuario autenticado tiene rol Administrador, el sistema debe usar el
commerceId enviado en la URL.
Route Params
Parámetro Tipo Requerido Descripción
de
dato
commerceId int Sí Identificador del comercio que recibirá el pago.
Para usuarios con rol Comercio, este valor debe
ignorarse y se debe usar el comercio asociado al
token.
Request Body
{
"cardNumber": "1589963258467598",
"monthExpirationCard": "02",
"yearExpirationCard": "2028",
"cvc": "859",
"transactionAmount": 689.25
}
Campos del body
Campo Tipo Requerido Descripción
de
dato
cardNumber string Sí Número de tarjeta de crédito de 16
dígitos.
monthExpirationCard string Sí Mes de expiración de la tarjeta. Debe
estar en formato MM.
yearExpirationCard string Sí Año de expiración de la tarjeta. Puede
recibirse en formato YYYY.

cvc string Sí Código de seguridad de 3 dígitos de la
tarjeta.
transactionAmount decim Sí Monto que se desea procesar como pago
al al comercio.
Validaciones
El endpoint debe cumplir las siguientes validaciones:
● El número de tarjeta es requerido.
● El número de tarjeta debe contener exactamente 16 dígitos.
● El mes de expiración es requerido.
● El mes de expiración debe tener un valor válido entre 01 y 12.
● El año de expiración es requerido.
● El CVC es requerido.
● El CVC debe contener exactamente 3 dígitos.
● El monto de la transacción es requerido.
● El monto de la transacción debe ser mayor que cero.
● La tarjeta debe existir en el sistema.
● La tarjeta debe estar activa.
● La tarjeta no debe estar vencida.
● El CVC recibido debe coincidir con el hash almacenado para la tarjeta.
● El comercio debe existir.
● El comercio debe estar activo.
● El comercio debe tener un usuario asociado.
● El usuario de comercio debe tener una cuenta de ahorro principal activa.
● La tarjeta debe tener crédito disponible suficiente para cubrir el monto de la
transacción.
Si faltan campos requeridos o tienen formato inválido, la API debe responder con
400 Bad Request.
Si la tarjeta no existe, está cancelada, está vencida o los datos no coinciden, la API
debe responder con 400 Bad Request.
Si el comercio no existe, la API debe responder con 404 Not Found.
Si el comercio existe, pero está inactivo, la API debe responder con 400 Bad
Request.
Validación del crédito disponible

Antes de aprobar el pago, el sistema debe calcular el crédito disponible de la
tarjeta.
El crédito disponible debe calcularse de la siguiente manera:
Crédito disponible = Límite de crédito de la tarjeta - Deuda actual de la tarjeta
Para aprobar el pago, el monto de la transacción no debe superar el crédito
disponible.
Ejemplo:
Si una tarjeta tiene un límite de RD$500.00 y una deuda actual de RD$300.00, el
crédito disponible será RD$200.00.
Si se intenta procesar una transacción por RD$201.00, el sistema debe rechazarla
porque supera el crédito disponible.
Si el monto de la transacción supera el crédito disponible, la API debe responder
con 400 Bad Request y mostrar un mensaje como el siguiente:
“El monto de la transacción excede el crédito disponible de la tarjeta.”
Procesamiento del pago
Si todas las validaciones son correctas, el sistema debe procesar el pago.
El sistema debe realizar las siguientes acciones:
● Aumentar la deuda de la tarjeta de crédito por el monto de la transacción.
● Actualizar el crédito disponible de la tarjeta.
● Registrar el consumo en el historial de la tarjeta con estado APROBADO.
● Acreditar el monto del pago en la cuenta de ahorro principal del comercio.
● Registrar una transacción de tipo CRÉDITO en la cuenta principal del comercio.
● Asociar el consumo al comercio correspondiente.
● Registrar la fecha y hora exacta de la operación.
El consumo registrado en la tarjeta debe contener como mínimo:
Campo Descripción
Tarjeta Tarjeta utilizada para realizar el
pago.

Comerci Comercio que recibió el pago.
o
Monto Monto procesado.
Fecha Fecha y hora de la transacción.
Estado APROBADO.
La transacción registrada en la cuenta principal del comercio debe contener como
mínimo:
Campo Valor
Tipo de CRÉDITO
transacción
Monto Monto recibido por el comercio
Origen Últimos cuatro dígitos de la tarjeta
utilizada
Beneficiario Número de cuenta principal del
comercio
Estado APROBADA
Fecha Fecha y hora de la operación
La operación debe ejecutarse de forma transaccional. Si ocurre un error al registrar
el consumo, actualizar la tarjeta o acreditar la cuenta del comercio, el sistema no
debe aplicar parcialmente la operación.
Registro de consumos rechazados
Si la tarjeta existe, pero el consumo es rechazado por falta de crédito disponible, el
sistema debe registrar el intento de consumo con estado RECHAZADO.
Este registro no debe aumentar la deuda de la tarjeta ni acreditar fondos al
comercio.
No se debe registrar transacción de crédito en la cuenta del comercio cuando el
consumo sea rechazado.

Correo al cliente propietario de la tarjeta
Una vez procesado correctamente el pago, el sistema debe enviar un correo
electrónico al cliente propietario de la tarjeta notificando el consumo.
El asunto del correo debe ser:
“Consumo realizado con la tarjeta [XXXX]”
Donde [XXXX] corresponde a los últimos cuatro dígitos de la tarjeta utilizada.
El cuerpo del correo debe incluir:
● Monto pagado.
● Últimos cuatro dígitos de la tarjeta.
● Nombre del comercio donde se realizó el consumo.
● Fecha de la transacción.
● Hora exacta de la transacción.
El correo puede tener un contenido como el siguiente:
Asunto: Consumo realizado con la tarjeta [XXXX]
Hola [Nombre del cliente],
Se ha realizado un consumo con su tarjeta terminada en [XXXX].
Comercio: [Nombre del comercio]
Monto: RD$[Monto]
Fecha y hora: [Fecha y hora]
Si usted no reconoce esta operación, comuníquese con la entidad bancaria.
Correo al comercio
Una vez procesado correctamente el pago, el sistema debe enviar un correo
electrónico al comercio notificando que ha recibido un nuevo pago.
El asunto del correo debe ser:
“Pago recibido a través de tarjeta [XXXX]”
Donde [XXXX] corresponde a los últimos cuatro dígitos de la tarjeta utilizada.
El cuerpo del correo debe incluir:
● Monto recibido.

 ● Últimos cuatro dígitos de la tarjeta utilizada para realizar el pago.
 ● Nombre del comercio receptor del pago.
 ● Fecha de la transacción.
 ● Hora exacta de la transacción.
El correo puede tener un contenido como el siguiente:
Asunto: Pago recibido a través de tarjeta [XXXX]
Hola [Nombre del comercio],
Ha recibido un nuevo pago mediante Hermes Pay.
Tarjeta terminada en: [XXXX]
 Monto recibido: RD$[Monto]
 Fecha y hora: [Fecha y hora]
Este mensaje sirve como constancia del pago recibido.
Si ocurre un error al enviar uno o ambos correos, el pago no debe revertirse. El
sistema debe registrar el error y mantener la operación aprobada.
Respuestas
| Código  | Resultado  |     | Descripción  |
| ------- | ---------- | --- | ------------ |
HTTP
| 204 No   | Pago       | El pago fue procesado correctamente.  |     |
| -------- | ---------- | ------------------------------------- | --- |
| Content  | procesado  |                                       |     |
400 Bad  Solicitud  Faltan campos requeridos, datos de tarjeta inválidos,
Request  inválida  comercio inactivo, tarjeta vencida o monto superior al crédito
disponible.
| 401         | No           | Token ausente, inválido o expirado.  |     |
| ----------- | ------------ | ------------------------------------ | --- |
| Unauthorize | autenticado  |                                      |     |
d
403  Acceso  El usuario autenticado no tiene rol Administrador o Comercio,
Forbidden  denegado  o no tiene permiso sobre el comercio indicado.
| 404 Not  | No          | El comercio indicado no existe.  |     |
| -------- | ----------- | -------------------------------- | --- |
| Found    | encontrado  |                                  |     |
Ejemplo de respuesta 400 Bad Request

{
"message": "El monto de la transacción excede el crédito disponible de la tarjeta."
}
Reglas adicionales del módulo
● Todos los endpoints de Hermes Pay requieren JWT.
● Solo los usuarios con rol Administrador o Comercio pueden consumir estos
endpoints.
● Si el usuario tiene rol Comercio, el commerceId debe obtenerse desde el token
JWT.
● Si el usuario tiene rol Administrador, el commerceId debe tomarse desde la URL.
● Un comercio inactivo no puede consultar ni procesar pagos.
● El usuario con rol Comercio solo puede operar sobre su propio comercio.
● El número de tarjeta recibido debe tener 16 dígitos.
● El CVC recibido debe validarse comparando su hash con el hash almacenado.
● El CVC no debe almacenarse ni retornarse en texto plano.
● La tarjeta debe estar activa y no vencida para procesar pagos.
● El monto de la transacción debe ser mayor que cero.
● La deuda de la tarjeta más el nuevo consumo no puede superar el límite de
crédito aprobado.
● Un pago aprobado debe aumentar la deuda de la tarjeta y reducir su crédito
disponible.
● Un pago aprobado debe acreditar el monto en la cuenta principal del comercio.
● Un pago aprobado debe registrar un consumo APROBADO en la tarjeta.
● Un pago aprobado debe registrar un CRÉDITO en la cuenta principal del
comercio.
● Un pago rechazado por falta de crédito disponible debe registrarse como
consumo RECHAZADO, sin modificar balances ni deudas.
● La operación debe ejecutarse de forma transaccional.
● El fallo en el envío de correos no debe revertir un pago aprobado.
Requerimientos técnicos
ViewModels y Validaciones
● Se deben utilizar ViewModels para la capa de presentación del web app.
● Todas las validaciones deben implementarse directamente en los

ViewModels, aprovechando las herramientas de validación del framework.
Persistencia de Datos
● Se debe utilizar Entity Framework Core con el enfoque Code First para la
gestión y persistencia de datos.
Interfaz de Usuario
● El proyecto debe contar con una interfaz intuitiva y visualmente clara,
utilizando Bootstrap como framework de diseño o cualquier otro framework
de css.
Arquitectura
● El proyecto debe implementar Onion Architecture, aplicada de manera
correcta y consistente al 100%.
● Cualquier error en su aplicación será considerado como una implementación
incorrecta.
Repositorios y Servicios
● Se deben crear repositorios genéricos y servicios genéricos para la gestión
de datos.
● Los servicios deben ser utilizados por los controladores de la WebApp.
Autenticación y Seguridad
El sistema debe utilizar ASP.NET Identity para la gestión de usuarios, roles,
contraseñas, activación de cuentas y restablecimiento de contraseña.
La Web App debe utilizar autenticación basada en sesión/cookies mediante
ASP.NET Identity.
La Web API debe utilizar autenticación basada en JWT.
Los tokens JWT deben incluir como mínimo:
● Identificador del usuario.
● Nombre de usuario.

● Rol.
● Fecha de emisión.
● Fecha de expiración.
Los usuarios inactivos no deben poder iniciar sesión ni generar tokens JWT.
Los usuarios creados desde el sistema deben quedar inicialmente inactivos, excepto
los usuarios creados por seeding para pruebas iniciales.
Los usuarios creados por seeding deben quedar activos para permitir el acceso
inicial al sistema.
Los tokens de activación y restablecimiento deben ser únicos, estar asociados a un
usuario y poder utilizarse una sola vez.
La información sensible debe almacenarse de forma segura.
El CVC de las tarjetas no debe almacenarse en texto plano. Debe almacenarse
como hash utilizando SHA-256 o un mecanismo de hashing seguro equivalente.
Mapeo de Datos
● Se debe utilizar AutoMapper para el mapeo entre ViewModels, Entities y
DTOs.
Patrones de Diseño
● Se deben implementar los patrones CQRS y Mediator en los endpoints de la
API.
● Se deben utilizar Behaviors para las validaciones de los Commands y
Queries, empleando FluentValidation.
Documentación de la API
● La API debe documentarse utilizando Swagger y sus herramientas de
generación de documentación.
Manejo de Excepciones
● Las excepciones deben manejarse de forma centralizada utilizando un Global

Exception Handler.
● Las respuestas de error deben generarse con el estándar Problem Details
(RFC 7807), tanto en la API como en la WebApp.
Pruebas Automatizadas
● Se deben implementar pruebas unitarias con xUnit para:
○ Commands y Queries de la capa de aplicación.
○ Servicios (services) de la lógica de negocio.
● Se deben realizar pruebas de integración para los repositorios, validando
las operaciones de persistencia con la base de datos.
Logs con Serilog
El sistema debe implementar logging utilizando Serilog.
Los logs deben registrar información relevante para auditoría, diagnóstico y
seguimiento de errores.
Serilog debe configurarse tanto en la Web App como en la Web API.
Los logs deben incluir, cuando aplique:
● Fecha y hora del evento.
● Nivel del log.
● Usuario autenticado que ejecutó la acción.
● Rol del usuario.
● Endpoint o acción ejecutada.
● Identificador de correlación de la solicitud.
● Resultado de la operación.
● Errores o excepciones ocurridas.
Las operaciones financieras relevantes deben generar logs informativos,
especialmente:
● Depósitos.
● Retiros.
● Transferencias.
● Pagos a tarjetas.

● Pagos a préstamos.
● Avances de efectivo.
● Procesamiento de pagos mediante Hermes Pay.
● Creación de préstamos.
● Asignación de tarjetas.
● Cancelación de productos financieros.
No se deben registrar en logs datos sensibles como:
● Contraseñas.
● Tokens JWT completos.
● Tokens de activación o restablecimiento.
● CVC de tarjetas.
● Hashes de CVC.
● Números completos de tarjetas.
● Cadenas de conexión.
● Secretos de configuración.
Cuando sea necesario identificar una tarjeta en logs, solo deben utilizarse los
últimos cuatro dígitos.
Los errores no controlados deben quedar registrados mediante Serilog antes de
retornar la respuesta correspondiente al usuario o consumidor de la API.
