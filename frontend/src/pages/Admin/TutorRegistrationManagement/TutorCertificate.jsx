import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import RemoveRedEyeIcon from '@mui/icons-material/RemoveRedEye';
import { Accordion, AccordionSummary, Box, Grid, IconButton, Modal, Typography } from '@mui/material';
import AccordionActions from '@mui/material/AccordionActions';
import AccordionDetails from '@mui/material/AccordionDetails';
import Button from '@mui/material/Button';
import { useEffect, useRef, useState } from 'react';
import HighlightOffIcon from '@mui/icons-material/HighlightOff';
import ZoomInIcon from '@mui/icons-material/ZoomIn';
import ZoomOutIcon from '@mui/icons-material/ZoomOut';
const image = [
    "https://thiepmung.com/uploads/worigin/2022/04/16/lam-giay-chung-nhan-thanh-tich-dep-nhat_9c193.jpg",
    "https://marketplace.canva.com/EAFlVDzb7sA/1/0/1600w/canva-white-gold-elegant-modern-certificate-of-participation-bK_WEelNCjo.jpg"
]
function TutorCertificate() {
    const [open, setOpen] = useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);
    const [openViewImage, setOpenViewImage] = useState(false);
    const [currentImage, setCurrentImage] = useState('');
    const [scale, setScale] = useState(1);
    const [position, setPosition] = useState({ x: 0, y: 0 });
    const imageRef = useRef(null);

    useEffect(() => {
        if (!openViewImage) {
            setScale(1);
            setPosition({
                x: 0,
                y: 0
            })
        }
    }, [openViewImage])
    useEffect(() => {
        const image = imageRef.current;
        let isDragging = false;
        let prevPosition = { x: 0, y: 0 };

        const handleMouseDown = (e) => {
            isDragging = true;
            prevPosition = { x: e.clientX, y: e.clientY };
        };

        const handleMouseMove = (e) => {
            if (!isDragging) return;
            const deltaX = e.clientX - prevPosition.x;
            const deltaY = e.clientY - prevPosition.y;
            prevPosition = { x: e.clientX, y: e.clientY };
            setPosition((position) => ({
                x: position.x + deltaX,
                y: position.y + deltaY,
            }));
        };

        const handleMouseUp = () => {
            isDragging = false;
        };

        image?.addEventListener("mousedown", handleMouseDown);
        image?.addEventListener("mousemove", handleMouseMove);
        image?.addEventListener("mouseup", handleMouseUp);

        return () => {
            image?.removeEventListener("mousedown", handleMouseDown);
            image?.removeEventListener("mousemove", handleMouseMove);
            image?.removeEventListener("mouseup", handleMouseUp);
        };
    }, [imageRef, scale]);
    const handleZoomIn = () => {
        if (scale <= 2) {
            setScale((scale) => scale + 0.1);
        }
    };

    const handleZoomOut = () => {
        if (scale > 1) {
            setScale((scale) => scale - 0.1);
        }
        if (scale === 1.1) {
            setPosition({
                x: 0,
                y: 0
            })
        }
    };
    return (
        <>
            <IconButton onClick={handleOpen}>
                <RemoveRedEyeIcon />
            </IconButton>
            <Modal
                open={open}
                onClose={handleClose}
            >
                <Box sx={{
                    position: 'absolute',
                    top: '50%',
                    left: '50%',
                    transform: 'translate(-50%, -50%)',
                    width: 600,
                    maxHeight: "90vh",
                    overflowY: "auto",
                    bgcolor: 'background.paper',
                    boxShadow: 24,
                    p: 4
                }}>
                    <Typography variant='h3' textAlign="center">Bằng cấp / Chứng chỉ</Typography>
                    <Box mt={3}>
                        <Accordion defaultExpanded>
                            <AccordionSummary
                                expandIcon={<ExpandMoreIcon />}
                                aria-controls="panel1-content"
                                id="panel1-header"
                            >
                                Bằng tốt nghiệp đại học FPT
                            </AccordionSummary>
                            <AccordionDetails>
                                <Grid container columnSpacing={2} rowSpacing={3}>
                                    <Grid item xs={3}>Nơi cấp:</Grid>
                                    <Grid item xs={9}>Nguyễn Văn A</Grid>
                                    <Grid item xs={3}>Ngày cấp:</Grid>
                                    <Grid item xs={9}>09-20-2002</Grid>
                                    <Grid item xs={3}>Ngày hết hạn:</Grid>
                                    <Grid item xs={9}>09-03-0303</Grid>
                                </Grid>
                                <Box sx={{ display: "flex", gap: 2, mt: 2 }}>
                                    <img style={{ width: "70px", height: "70px", cursor: "pointer" }}
                                        onClick={() => { setCurrentImage(image[0]); setOpenViewImage(true) }}
                                        src={image[0]} />
                                    <img style={{ width: "70px", height: "70px", cursor: "pointer" }}
                                        onClick={() => { setCurrentImage(image[1]); setOpenViewImage(true) }}
                                        src={image[1]} />
                                </Box>
                                <AccordionActions>
                                    <Button>Từ chối</Button>
                                </AccordionActions>
                            </AccordionDetails>
                        </Accordion>
                        <Accordion>
                            <AccordionSummary
                                expandIcon={<ExpandMoreIcon />}
                                aria-controls="panel2-content"
                                id="panel2-header"
                            >
                                Accordion 2
                            </AccordionSummary>
                            <AccordionDetails>
                                Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse
                                malesuada lacus ex, sit amet blandit leo lobortis eget.
                            </AccordionDetails>
                        </Accordion>
                        <Accordion >
                            <AccordionSummary
                                expandIcon={<ExpandMoreIcon />}
                                aria-controls="panel3-content"
                                id="panel3-header"
                            >
                                Accordion Actions
                            </AccordionSummary>
                            <AccordionDetails>
                                Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse
                                malesuada lacus ex, sit amet blandit leo lobortis eget.
                            </AccordionDetails>
                            <AccordionActions>
                                <Button>Cancel</Button>
                                <Button>Agree</Button>
                            </AccordionActions>
                        </Accordion>
                    </Box>
                </Box>
            </Modal>

            {
                currentImage && (
                    <Modal open={openViewImage} onClose={() => setOpenViewImage(false)}>
                        <Box
                            display="flex"
                            justifyContent="center"
                            alignItems="center"
                            height="100vh"
                            width="100vw"
                            bgcolor="rgba(0, 0, 0, 0.8)"
                            position="relative"
                        >
                            <Box sx={{
                                position: 'absolute',
                                top: "50%",
                                left: "50%",
                                transform: 'translate(-50%, -50%)',
                                height: "85vh",
                                width: "90vw",
                                overflow: 'hidden'
                            }}>
                                <img
                                    src={currentImage}
                                    alt="large"
                                    style={{
                                        maxWidth: '90%',
                                        maxHeight: '90%',
                                        transform: `translate(-50%, -50%) scale(${scale}) translate(${position.x}px, ${position.y}px)`,
                                        position: 'absolute',
                                        top: "50%",
                                        left: "50%",
                                        cursor: "move"
                                    }}
                                    ref={imageRef}
                                />
                            </Box>
                            <IconButton
                                onClick={() => setOpenViewImage(false)}
                                style={{ position: 'absolute', top: 20, right: 20, color: 'white' }}
                            >
                                <HighlightOffIcon />
                            </IconButton>
                            <IconButton
                                onClick={() => handleZoomOut(false)}
                                style={{ position: 'absolute', top: 20, right: 60, color: 'white' }}
                            >
                                <ZoomOutIcon />
                            </IconButton>
                            <IconButton
                                onClick={() => handleZoomIn(false)}
                                style={{ position: 'absolute', top: 20, right: 100, color: 'white' }}
                            >
                                <ZoomInIcon />
                            </IconButton>
                        </Box>
                    </Modal >
                )
            }
        </>
    )
}

export default TutorCertificate
