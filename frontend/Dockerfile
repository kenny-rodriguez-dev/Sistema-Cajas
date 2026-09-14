# ═══ ETAPA 1: DEPENDENCIAS ═══
FROM node:22-alpine AS dependencias
# "alpine" es una version minima de Linux: la imagen pesa mucho menos.

WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci
# npm ci y no npm install: instala EXACTAMENTE las versiones del
# package-lock.json, sin actualizar nada. En un build reproducible
# eso no es opcional: la imagen de hoy y la de manana deben tener
# las mismas versiones.

# ═══ ETAPA 2: COMPILAR ═══
FROM node:22-alpine AS compilacion
WORKDIR /app

COPY --from=dependencias /app/node_modules ./node_modules
COPY . .

ARG NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
# Todo lo que lleva el prefijo NEXT_PUBLIC_ se "hornea" DENTRO del
# JavaScript durante el build. No se lee al arrancar el contenedor:
# queda escrito en el codigo que viaja al navegador.
# Por eso va como ARG (valor que se pasa AL CONSTRUIR).
# Consecuencia: si cambias esta URL hay que RECONSTRUIR la imagen,
# no basta reiniciar el contenedor.

RUN npm run build

# ═══ ETAPA 3: EJECUTAR ═══
FROM node:22-alpine AS final
WORKDIR /app

ENV NODE_ENV=production

COPY --from=compilacion /app/public ./public
COPY --from=compilacion /app/.next/standalone ./
COPY --from=compilacion /app/.next/static ./.next/static
# "standalone" es un modo de Next.js que arma un servidor minimo con
# solo los node_modules que realmente se usan. Sin el habria que
# copiar los 300 MB de node_modules completos.

EXPOSE 3000
ENV PORT=3000
ENV HOSTNAME=0.0.0.0
# Por defecto escucharia solo en localhost, que dentro de un
# contenedor significa "solo yo mismo" y no sirve de nada.

CMD ["node", "server.js"]