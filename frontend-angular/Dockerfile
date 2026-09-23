# ═══ ETAPA 1: COMPILAR ═══
FROM node:22-alpine AS compilacion

WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci
# Primero las dependencias, despues el codigo: asi Docker cachea
# la instalacion y los builds siguientes se la saltan.
# Mismo truco que en los otros dos Dockerfiles.

COPY . .

RUN npx ng build --configuration production
# --configuration production hace dos cosas:
#   1. Minifica y optimiza el codigo
#   2. Usa environment.ts en vez de environment.development.ts
# Ese segundo punto es el que decide a que URL llama el frontend.

# ═══ ETAPA 2: SERVIR ═══
FROM nginx:alpine AS final
# De aqui en adelante, NO HAY NODE. Solo nginx.
# La etapa de compilacion, con sus 300 MB de node_modules,
# se queda atras y no viaja a la imagen final.

COPY nginx.conf /etc/nginx/conf.d/default.conf
# Reemplaza la configuracion por defecto de nginx por la tuya.

COPY --from=compilacion /app/dist/frontend-angular/browser /usr/share/nginx/html
# Trae SOLO los archivos compilados desde la etapa anterior.
# Si tu build genero otra ruta, ajustala aqui: es la que viste
# con el ng build local.

EXPOSE 80
# nginx:alpine ya trae su propio comando de arranque.
# No hace falta un CMD.