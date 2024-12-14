import React, { useState } from 'react';
import { Dialog, DialogContent, Button, IconButton, ImageList, ImageListItem, Typography } from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';

function srcset(image, size, rows = 1, cols = 1) {
    return {
        src: `${image}?w=${size * cols}&h=${size * rows}&fit=crop&auto=format`,
        srcSet: `${image}?w=${size * cols}&h=${size * rows
            }&fit=crop&auto=format&dpr=2 2x`,
    };
}
const ImageGallery = ({ images }) => {
    const [open, setOpen] = useState(false);
    const [currentIndex, setCurrentIndex] = useState(0);

    const handleClickOpen = (index) => {
        setCurrentIndex(index);
        setOpen(true);
    };

    const handleClose = () => {
        setOpen(false);
    };

    const handlePrev = () => {
        setCurrentIndex((prevIndex) => (prevIndex === 0 ? images.length - 1 : prevIndex - 1));
    };

    const handleNext = () => {
        setCurrentIndex((prevIndex) => (prevIndex === images.length - 1 ? 0 : prevIndex + 1));
    };

    return (
        <div>
            <ImageList
                sx={{ width: "100%" }}
                variant="quilted"
                cols={4}
                rowHeight={121}
            >
                {images.map((item, index) => (
                    <ImageListItem key={item.img} cols={item.cols || 1} rows={item.rows || 1}
                        sx={{
                            cursor: "pointer"
                        }}
                    >
                        <img
                            {...srcset(item.img, 121, item.rows, item.cols)}
                            alt={item.title}
                            onClick={() => handleClickOpen(index)}
                        />
                    </ImageListItem>
                ))}
            </ImageList>
            <Dialog open={open} onClose={handleClose} >
                <DialogContent style={{ textAlign: 'center' }}>
                    <img src={images[currentIndex].img} alt={`Image ${currentIndex + 1}`} style={{ maxWidth: '100%', maxHeight: "100%" }} />
                </DialogContent>
                <div style={{ display: 'flex', justifyContent: 'space-between', padding: '10px' }}>
                    <IconButton onClick={handlePrev}>
                        <ArrowBackIcon />
                    </IconButton>
                    <Typography>{currentIndex + 1} / {images.length}</Typography>
                    <IconButton onClick={handleNext}>
                        <ArrowForwardIcon />
                    </IconButton>
                </div>
            </Dialog>
        </div>
    );
};

export default ImageGallery;
