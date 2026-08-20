set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
RID="${1:?A Linux runtime identifier is required}"
VERSION="${2:-}"
FORMAT="${3:?A package format is required}"
OUTPUT="${4:?An output directory is required}"

if [ -z "$VERSION" ]; then
  VERSION="$(sed -n 's:^[[:space:]]*<XenostrapVersion>\(.*\)</XenostrapVersion>[[:space:]]*$:\1:p' "$ROOT/Directory.Build.props" | head -n 1)"
fi

case "$RID" in
  linux-x64) DEB_ARCH="amd64"; RPM_ARCH="x86_64"; APPIMAGE_ARCH="x86_64" ;;
  linux-arm64) DEB_ARCH="arm64"; RPM_ARCH="aarch64"; APPIMAGE_ARCH="aarch64" ;;
  linux-musl-x64) DEB_ARCH="amd64"; RPM_ARCH="x86_64"; APPIMAGE_ARCH="x86_64" ;;
  linux-musl-arm64) DEB_ARCH="arm64"; RPM_ARCH="aarch64"; APPIMAGE_ARCH="aarch64" ;;
  *) echo "Unsupported Linux runtime identifier"; exit 1 ;;
esac

case "$FORMAT" in
  deb|rpm|appimage|tar) ;;
  *) echo "Unsupported package format"; exit 1 ;;
esac

case "$RID:$FORMAT" in
  linux-musl-x64:deb|linux-musl-x64:rpm|linux-musl-x64:appimage|linux-musl-arm64:deb|linux-musl-arm64:rpm|linux-musl-arm64:appimage)
    echo "This package format requires a glibc runtime identifier"
    exit 1
    ;;
esac

case "$FORMAT" in
  deb) command -v dpkg-deb >/dev/null 2>&1 || { echo "The Debian package tool is unavailable"; exit 1; } ;;
  rpm) command -v rpmbuild >/dev/null 2>&1 || { echo "The RPM package tool is unavailable"; exit 1; } ;;
  appimage) command -v appimagetool >/dev/null 2>&1 || { echo "The AppImage package tool is unavailable"; exit 1; } ;;
  tar) command -v tar >/dev/null 2>&1 || { echo "The tar package tool is unavailable"; exit 1; } ;;
esac

if [[ ! "$VERSION" =~ ^[0-9]+([.][0-9]+){1,3}$ ]]; then
  echo "The package version is invalid"
  exit 1
fi

mkdir -p "$OUTPUT"
OUTPUT="$(cd "$OUTPUT" && pwd)"
case "$FORMAT" in
  deb) FINAL_TARGET="$OUTPUT/Xenostrap_${VERSION}_${DEB_ARCH}.deb" ;;
  rpm) FINAL_TARGET="$OUTPUT/Xenostrap_${VERSION}_${RPM_ARCH}.rpm" ;;
  appimage) FINAL_TARGET="$OUTPUT/Xenostrap_${VERSION}_${APPIMAGE_ARCH}.AppImage" ;;
  tar) FINAL_TARGET="$OUTPUT/Xenostrap_${VERSION}_${RID}.tar.gz" ;;
esac
if [ -e "$FINAL_TARGET" ]; then
  echo "The requested output already exists"
  exit 1
fi
LOCK="$OUTPUT/.xenostrap-linux-$RID.lock"
if ! mkdir "$LOCK" 2>/dev/null; then
  echo "Another package operation is already running"
  exit 1
fi
STAGE=""

cleanup() {
	if [ -n "$STAGE" ] && [ -e "$STAGE" ]; then
		rm -rf "$STAGE"
	fi
	rmdir "$LOCK" 2>/dev/null || true
}

trap cleanup EXIT
STAGE="$(mktemp -d "$OUTPUT/.xenostrap-linux.XXXXXX")"
PUBLISH="$STAGE/publish"
mkdir -p "$PUBLISH"
dotnet publish "$ROOT/src/Xenostrap.Cross/Xenostrap.Cross.csproj" -c Release -r "$RID" --self-contained true -o "$PUBLISH" -p:BaseIntermediateOutputPath="obj-$RID/" -p:Version="$VERSION" -p:DebugType=none -p:DebugSymbols=false
test -s "$PUBLISH/Xenostrap"
if [ "$(find "$PUBLISH" -mindepth 1 | wc -l)" -ne 1 ]; then
  echo "The Linux publish is not a single file"
  find "$PUBLISH" -mindepth 1
  exit 1
fi
chmod 755 "$PUBLISH/Xenostrap"

case "$FORMAT" in
  deb)
    TARGET="$STAGE/Xenostrap_${VERSION}_${DEB_ARCH}.deb"
    ROOTFS="$STAGE/deb"
    mkdir -p "$ROOTFS/DEBIAN" "$ROOTFS/usr/lib/xenostrap" "$ROOTFS/usr/bin" "$ROOTFS/usr/share/applications" "$ROOTFS/usr/share/icons/hicolor/256x256/apps"
    cp -R "$PUBLISH/." "$ROOTFS/usr/lib/xenostrap/"
    ln -s /usr/lib/xenostrap/Xenostrap "$ROOTFS/usr/bin/xenostrap"
    cp "$ROOT/build/Packaging/Linux/xenostrap.desktop" "$ROOTFS/usr/share/applications/xenostrap.desktop"
    cp "$ROOT/src/Xenostrap.App/Xenostrap.png" "$ROOTFS/usr/share/icons/hicolor/256x256/apps/xenostrap.png"
    printf 'Package: xenostrap\nVersion: %s\nSection: games\nPriority: optional\nArchitecture: %s\nMaintainer: Xenostrap\nDepends: libc6, libgcc-s1 | libgcc1, libstdc++6, libx11-6, libice6, libsm6, libfontconfig1, libfreetype6, libgl1 | libgl1-mesa-glx, libegl1 | libegl1-mesa, zlib1g, ca-certificates\nRecommends: xdg-utils, libnotify-bin, libsecret-tools, libgtk-3-0, libvulkan1, libwayland-client0, libwebkit2gtk-4.1-0 | libwpewebkit-2.0-1\nDescription: Xenostrap Roblox desktop launcher\n' "$VERSION" "$DEB_ARCH" > "$ROOTFS/DEBIAN/control"
    dpkg-deb --root-owner-group --build "$ROOTFS" "$TARGET"
    ;;
  rpm)
    TARGET="$STAGE/Xenostrap_${VERSION}_${RPM_ARCH}.rpm"
    RPMROOT="$STAGE/rpmbuild"
    mkdir -p "$RPMROOT/BUILD" "$RPMROOT/BUILDROOT" "$RPMROOT/RPMS" "$RPMROOT/SOURCES" "$RPMROOT/SPECS" "$RPMROOT/SRPMS"
    cp -R "$PUBLISH/." "$RPMROOT/SOURCES/publish"
    cp "$ROOT/build/Packaging/Linux/xenostrap.desktop" "$RPMROOT/SOURCES/xenostrap.desktop"
    cp "$ROOT/src/Xenostrap.App/Xenostrap.png" "$RPMROOT/SOURCES/xenostrap.png"
    printf 'Name: xenostrap\nVersion: %s\nRelease: 1\nSummary: Xenostrap Roblox desktop launcher\nLicense: Custom\nBuildArch: %s\nRequires: libX11\nRequires: libICE\nRequires: libSM\nRequires: fontconfig\nRequires: freetype\nRequires: mesa-libGL\nRequires: mesa-libEGL\nRequires: ca-certificates\nRecommends: xdg-utils\nRecommends: libnotify\nRecommends: libsecret\nRecommends: vulkan-loader\nRecommends: webkit2gtk4.1\nSource0: publish\nSource1: xenostrap.desktop\nSource2: xenostrap.png\n%%description\nXenostrap Roblox desktop launcher\n%%install\nmkdir -p %%{buildroot}%%{_libdir}/xenostrap %%{buildroot}%%{_bindir} %%{buildroot}%%{_datadir}/applications %%{buildroot}%%{_datadir}/icons/hicolor/256x256/apps\ncp -a %%{_sourcedir}/publish/. %%{buildroot}%%{_libdir}/xenostrap/\nln -s %%{_libdir}/xenostrap/Xenostrap %%{buildroot}%%{_bindir}/xenostrap\ninstall -m 644 %%{_sourcedir}/xenostrap.desktop %%{buildroot}%%{_datadir}/applications/xenostrap.desktop\ninstall -m 644 %%{_sourcedir}/xenostrap.png %%{buildroot}%%{_datadir}/icons/hicolor/256x256/apps/xenostrap.png\n%%files\n%%{_libdir}/xenostrap\n%%{_bindir}/xenostrap\n%%{_datadir}/applications/xenostrap.desktop\n%%{_datadir}/icons/hicolor/256x256/apps/xenostrap.png\n' "$VERSION" "$RPM_ARCH" > "$RPMROOT/SPECS/xenostrap.spec"
    rpmbuild --define "_topdir $RPMROOT" --target "$RPM_ARCH" -bb "$RPMROOT/SPECS/xenostrap.spec"
    cp "$RPMROOT/RPMS/$RPM_ARCH/xenostrap-$VERSION-1.$RPM_ARCH.rpm" "$TARGET"
    ;;
  appimage)
    TARGET="$STAGE/Xenostrap_${VERSION}_${APPIMAGE_ARCH}.AppImage"
    APPDIR="$STAGE/AppDir"
    mkdir -p "$APPDIR/usr/lib/xenostrap" "$APPDIR/usr/bin"
    cp -R "$PUBLISH/." "$APPDIR/usr/lib/xenostrap/"
    ln -s ../lib/xenostrap/Xenostrap "$APPDIR/usr/bin/xenostrap"
    ln -s usr/bin/xenostrap "$APPDIR/AppRun"
    cp "$ROOT/build/Packaging/Linux/xenostrap.desktop" "$APPDIR/xenostrap.desktop"
    cp "$ROOT/src/Xenostrap.App/Xenostrap.png" "$APPDIR/xenostrap.png"
    ARCH="$APPIMAGE_ARCH" appimagetool "$APPDIR" "$TARGET"
    ;;
  tar)
    TARGET="$STAGE/Xenostrap_${VERSION}_${RID}.tar.gz"
    BUNDLE="$STAGE/Xenostrap"
    mkdir -p "$BUNDLE"
    cp "$PUBLISH/Xenostrap" "$BUNDLE/Xenostrap"
    chmod 755 "$BUNDLE/Xenostrap"
    tar --sort=name --mtime="@${SOURCE_DATE_EPOCH:-0}" --owner=0 --group=0 --numeric-owner -C "$STAGE" -czf "$TARGET" Xenostrap
    ;;
esac

mv -n "$TARGET" "$FINAL_TARGET"
if [ -e "$TARGET" ]; then
  echo "The requested output already exists"
  exit 1
fi
