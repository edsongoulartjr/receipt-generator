import { Injectable } from '@angular/core';
import { Capacitor } from '@capacitor/core';
import { Filesystem, Directory } from '@capacitor/filesystem';
import { Share } from '@capacitor/share';

@Injectable({
  providedIn: 'root'
})
export class ShareService {
  /**
   * Compartilha um arquivo PDF.
   * - Em contexto nativo (Capacitor/Android): salva no cache e usa o Intent nativo de compartilhamento.
   * - Em browser: tenta Web Share API com arquivo; fallback para download.
   *
   * @returns true se compartilhado via share sheet; false se foi feito download (fallback)
   */
  async sharePdf(
    blob: Blob,
    fileName: string,
    title: string,
    text: string
  ): Promise<boolean> {
    if (Capacitor.isNativePlatform()) {
      return this.shareViaNative(blob, fileName, title, text);
    }
    return this.shareViaWebApi(blob, fileName, title, text);
  }

  private async shareViaNative(
    blob: Blob,
    fileName: string,
    title: string,
    text: string
  ): Promise<boolean> {
    const base64 = await this.blobToBase64(blob);

    await Filesystem.writeFile({
      path: fileName,
      data: base64,
      directory: Directory.Cache
    });

    const { uri } = await Filesystem.getUri({
      path: fileName,
      directory: Directory.Cache
    });

    try {
      await Share.share({ title, text, url: uri, dialogTitle: 'Compartilhar recibo' });
      return true;
    } finally {
      // Remove o arquivo temporário após o share sheet fechar
      Filesystem.deleteFile({ path: fileName, directory: Directory.Cache }).catch(() => {});
    }
  }

  private async shareViaWebApi(
    blob: Blob,
    fileName: string,
    title: string,
    text: string
  ): Promise<boolean> {
    const file = new File([blob], fileName, { type: 'application/pdf' });

    if (navigator.share && navigator.canShare?.({ files: [file] })) {
      await navigator.share({ title, text, files: [file] });
      return true;
    }

    this.downloadFile(blob, fileName);
    return false;
  }

  private blobToBase64(blob: Blob): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => {
        const dataUrl = reader.result as string;
        resolve(dataUrl.split(',')[1]);
      };
      reader.onerror = reject;
      reader.readAsDataURL(blob);
    });
  }

  private downloadFile(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
}
