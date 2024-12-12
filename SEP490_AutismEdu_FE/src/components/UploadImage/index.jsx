import UploadIcon from '@mui/icons-material/Upload';
import { Box, Button, FormHelperText, Modal } from '@mui/material';
import { useEffect, useRef, useState } from 'react';
import ReactCrop, { centerCrop, convertToPixelCrop, makeAspectCrop } from 'react-image-crop';

function UploadImage({ setImage, aspectRatio, minDimension }) {
    const [open, setOpen] = useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);
    const inputFile = useRef();
    const imgRef = useRef(null);
    const previewCanvasRef = useRef(null);
    const [imgSrc, setImgSrc] = useState("");
    const [crop, setCrop] = useState();
    const [error, setError] = useState("");
    const handleChooseFile = () => {
        if (inputFile) {
            inputFile.current.click();
        }
    }

    useEffect(() => {
        if (open) {
            setImgSrc("");
        }
    }, [open])
    const onSelectFile = (e) => {
        const file = e.target.files?.[0];
        if (!file) return;

        const reader = new FileReader();
        reader.addEventListener("load", () => {
            const imageElement = new Image();
            const imageUrl = reader.result?.toString() || "";
            imageElement.src = imageUrl;

            imageElement.addEventListener("load", (e) => {
                if (error) setError("");
                const { naturalWidth, naturalHeight } = e.currentTarget;
                if (naturalWidth < minDimension || naturalHeight < minDimension) {
                    setError(`Image must be at least ${minDimension} x ${minDimension} pixels.`);
                    return setImgSrc("");
                }
            });
            setImgSrc(imageUrl);
        });
        reader.readAsDataURL(file);
    };
    const setCanvasPreview = (
        image,
        canvas,
        crop
    ) => {
        const ctx = canvas.getContext("2d");
        if (!ctx) {
            throw new Error("No 2d context");
        }
        const pixelRatio = window.devicePixelRatio;
        const scaleX = image.naturalWidth / image.width;
        const scaleY = image.naturalHeight / image.height;

        canvas.width = Math.floor(crop.width * scaleX * pixelRatio);
        canvas.height = Math.floor(crop.height * scaleY * pixelRatio);

        ctx.scale(pixelRatio, pixelRatio);
        ctx.imageSmoothingQuality = "high";
        ctx.save();

        const cropX = crop.x * scaleX;
        const cropY = crop.y * scaleY;
        ctx.translate(-cropX, -cropY);
        ctx.drawImage(
            image,
            0,
            0,
            image.naturalWidth,
            image.naturalHeight,
            0,
            0,
            image.naturalWidth,
            image.naturalHeight
        );
        ctx.restore();
    };

    const onImageLoad = (e) => {
        const { width, height } = e.currentTarget;
        const cropWidthInPercent = (minDimension / width) * 100;

        const crop = makeAspectCrop(
            {
                unit: "%",
                width: cropWidthInPercent,
            },
            aspectRatio,
            width,
            height
        );
        const centeredCrop = centerCrop(crop, width, height);
        setCrop(centeredCrop);
    };

    const dataURLtoBlob = (dataURL) => {
        const byteString = atob(dataURL.split(',')[1]);
        const mimeString = dataURL.split(',')[0].split(':')[1].split(';')[0];
        const ab = new ArrayBuffer(byteString.length);
        const ia = new Uint8Array(ab);

        for (let i = 0; i < byteString.length; i++) {
            ia[i] = byteString.charCodeAt(i);
        }

        return new Blob([ab], { type: mimeString });
    };

    const blobToFile = (blob, fileName) => {
        return new File([blob], fileName, { type: blob.type });
    };
    return (
        <>
            <Button startIcon={<UploadIcon />} onClick={handleOpen}>Upload hình ảnh</Button>
            <Modal
                open={open}
                onClose={handleClose}
                aria-labelledby="modal-modal-title"
                aria-describedby="modal-modal-description"
            >
                <Box sx={{
                    position: 'absolute',
                    top: '50%',
                    left: '50%',
                    transform: 'translate(-50%, -50%)',
                    width: 500,
                    height: 500,
                    bgcolor: 'background.paper',
                    borderRadius: "10px",
                    boxShadow: 24,
                    p: 4,
                    display: "flex",
                    justifyContent: "center",
                    alignItems: "center"
                }}>
                    {
                        imgSrc === '' &&
                        <Box sx={{ textAlign: "center" }}>
                            <div>
                                <svg aria-label="Icon to represent media such as images or videos" className="x1lliihq x1n2onr6 x5n08af" fill="currentColor" height="77" role="img" viewBox="0 0 97.6 77.3" width="96"><title>Icon to represent media such as images or videos</title><path d="M16.3 24h.3c2.8-.2 4.9-2.6 4.8-5.4-.2-2.8-2.6-4.9-5.4-4.8s-4.9 2.6-4.8 5.4c.1 2.7 2.4 4.8 5.1 4.8zm-2.4-7.2c.5-.6 1.3-1 2.1-1h.2c1.7 0 3.1 1.4 3.1 3.1 0 1.7-1.4 3.1-3.1 3.1-1.7 0-3.1-1.4-3.1-3.1 0-.8.3-1.5.8-2.1z" fill="currentColor"></path><path d="M84.7 18.4 58 16.9l-.2-3c-.3-5.7-5.2-10.1-11-9.8L12.9 6c-5.7.3-10.1 5.3-9.8 11L5 51v.8c.7 5.2 5.1 9.1 10.3 9.1h.6l21.7-1.2v.6c-.3 5.7 4 10.7 9.8 11l34 2h.6c5.5 0 10.1-4.3 10.4-9.8l2-34c.4-5.8-4-10.7-9.7-11.1zM7.2 10.8C8.7 9.1 10.8 8.1 13 8l34-1.9c4.6-.3 8.6 3.3 8.9 7.9l.2 2.8-5.3-.3c-5.7-.3-10.7 4-11 9.8l-.6 9.5-9.5 10.7c-.2.3-.6.4-1 .5-.4 0-.7-.1-1-.4l-7.8-7c-1.4-1.3-3.5-1.1-4.8.3L7 49 5.2 17c-.2-2.3.6-4.5 2-6.2zm8.7 48c-4.3.2-8.1-2.8-8.8-7.1l9.4-10.5c.2-.3.6-.4 1-.5.4 0 .7.1 1 .4l7.8 7c.7.6 1.6.9 2.5.9.9 0 1.7-.5 2.3-1.1l7.8-8.8-1.1 18.6-21.9 1.1zm76.5-29.5-2 34c-.3 4.6-4.3 8.2-8.9 7.9l-34-2c-4.6-.3-8.2-4.3-7.9-8.9l2-34c.3-4.4 3.9-7.9 8.4-7.9h.5l34 2c4.7.3 8.2 4.3 7.9 8.9z" fill="currentColor"></path><path d="M78.2 41.6 61.3 30.5c-2.1-1.4-4.9-.8-6.2 1.3-.4.7-.7 1.4-.7 2.2l-1.2 20.1c-.1 2.5 1.7 4.6 4.2 4.8h.3c.7 0 1.4-.2 2-.5l18-9c2.2-1.1 3.1-3.8 2-6-.4-.7-.9-1.3-1.5-1.8zm-1.4 6-18 9c-.4.2-.8.3-1.3.3-.4 0-.9-.2-1.2-.4-.7-.5-1.2-1.3-1.1-2.2l1.2-20.1c.1-.9.6-1.7 1.4-2.1.8-.4 1.7-.3 2.5.1L77 43.3c1.2.8 1.5 2.3.7 3.4-.2.4-.5.7-.9.9z" fill="currentColor"></path></svg>
                            </div>
                            <p>Tải ảnh đại diện của bạn</p>
                            <Button className='secondary' variant='contained' onClick={handleChooseFile}>Chọn ảnh</Button>
                            <input type='file' style={{ display: "none" }} multiple accept='image/*' onChange={onSelectFile} ref={inputFile} />
                            {
                                error && <FormHelperText error>
                                    Hình ảnh quá nhỏ, vui lòng chọn ảnh khác!
                                </FormHelperText>
                            }
                        </Box>
                    }
                    {imgSrc && (
                        <div className="flex flex-col items-center">
                            <Box>
                                <ReactCrop
                                    crop={crop}
                                    onChange={(pixelCrop, percentCrop) => setCrop(percentCrop)}
                                    circularCrop
                                    keepSelection
                                    aspect={aspectRatio}
                                    minWidth={minDimension}
                                >
                                    <img
                                        ref={imgRef}
                                        src={imgSrc}
                                        alt="Upload"
                                        style={{ maxHeight: "300px", maxWidth: "100%" }}
                                        onLoad={onImageLoad}
                                    />
                                </ReactCrop>
                            </Box>
                            <Button
                                variant='contained'
                                className="text-white font-mono text-xs py-2 px-4 rounded-2xl mt-4 bg-sky-500 hover:bg-sky-600"
                                onClick={() => {
                                    setCanvasPreview(
                                        imgRef.current,
                                        previewCanvasRef.current,
                                        convertToPixelCrop(
                                            crop,
                                            imgRef.current.width,
                                            imgRef.current.height
                                        )
                                    );
                                    const dataUrl = previewCanvasRef.current.toDataURL();
                                    const blob = dataURLtoBlob(dataUrl);
                                    const file = blobToFile(blob, 'avatar.jpg');
                                    setImage(file)
                                    handleClose()
                                }}
                            >
                                Cắt hình ảnh
                            </Button>
                            {crop && (
                                <canvas
                                    ref={previewCanvasRef}
                                    className="mt-4"
                                    style={{
                                        display: "none",
                                        border: "1px solid black",
                                        objectFit: "contain",
                                        width: 150,
                                        height: 150,
                                    }}
                                />
                            )}
                        </div>
                    )}
                </Box>
            </Modal>
        </>
    )
}

export default UploadImage
