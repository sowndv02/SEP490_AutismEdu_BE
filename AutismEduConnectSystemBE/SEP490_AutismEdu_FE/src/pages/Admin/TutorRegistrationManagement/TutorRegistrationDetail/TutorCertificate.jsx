import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import HighlightOffIcon from '@mui/icons-material/HighlightOff';
import ZoomInIcon from '@mui/icons-material/ZoomIn';
import ZoomOutIcon from '@mui/icons-material/ZoomOut';
import { Accordion, AccordionSummary, Box, Grid, IconButton, Modal, Paper, Stack, Typography } from '@mui/material';
import AccordionActions from '@mui/material/AccordionActions';
import AccordionDetails from '@mui/material/AccordionDetails';
import { useEffect, useRef, useState } from 'react';
import RejectCertificate from '../handleDialog/RejectCertificate';
import SchoolIcon from '@mui/icons-material/School';
function TutorCertificate({ id, certificates, setCertificates }) {
    const [openViewImage, setOpenViewImage] = useState(false);
    const [currentImage, setCurrentImage] = useState('');
    const [scale, setScale] = useState(1);
    const [position, setPosition] = useState({ x: 0, y: 0 });
    const imageRef = useRef(null);
    const [displayList, setDisplayList] = useState([]);
    useEffect(() => {
        if (certificates) {
            const list = certificates.filter((c) => {
                return c.certificateName !== "Căn cước công dân"
            })
            setDisplayList(list);
        }
    }, [certificates])
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

    const formatDate = (date) => {
        const dateObj = new Date(date);
        const formattedDate = dateObj.getDate().toString().padStart(2, '0') + '/' +
            (dateObj.getMonth() + 1).toString().padStart(2, '0') + '/' +
            dateObj.getFullYear();
        return formattedDate;
    }
    return (
        <>
            <Paper variant='elevation' sx={{ p: 2 }}>
                <Stack direction='row' mb={2} gap={2} bgcolor="#E3F2FD" p={1} borderRadius="5px"
                    sx={{
                        border: "1px solid #BBDEFB"
                    }}
                >
                    <SchoolIcon sx={{ color: "#0056B3" }} />
                    <Typography variant='h5' color="#0056B3">Các bằng cấp, chứng chỉ</Typography>
                </Stack>
                {
                    displayList?.map((c, index) => {
                        return (
                            <Accordion defaultExpanded={index === 0} key={c.id}>
                                <AccordionSummary
                                    expandIcon={<ExpandMoreIcon />}
                                    aria-controls="panel1-content"
                                    id="panel1-header"
                                    sx={{ lineHeight: "20px" }}
                                >
                                    <span style={{ fontWeight: "bold", fontSize: "20px" }}>{c.certificateName} </span>{
                                        c.requestStatus === 0 && <span style={{ color: "red", marginLeft: "20px" }}>(Đã từ chối)</span>
                                    }
                                    {
                                        c.requestStatus === 1 && <span style={{ color: "green", marginLeft: "20px" }}>(Đã chấp nhận)</span>
                                    }
                                    {
                                        c.requestStatus === 2 && <span style={{ color: "blue", marginLeft: "20px" }}>(Đang chờ)</span>
                                    }
                                </AccordionSummary>
                                <AccordionDetails>
                                    <Grid container columnSpacing={2} rowSpacing={3}>
                                        <Grid item xs={3} style={{ fontWeight: "bold" }}>Nơi cấp:</Grid>
                                        <Grid item xs={9}>{c.issuingInstitution}</Grid>
                                        <Grid item xs={3} style={{ fontWeight: "bold" }}>Ngày cấp:</Grid>
                                        <Grid item xs={9}>{formatDate(c.issuingDate)}</Grid>
                                        {
                                            c.expirationDate && (
                                                <>
                                                    <Grid item xs={3} style={{ fontWeight: "bold" }}>Ngày hết hạn:</Grid>
                                                    <Grid item xs={9}>{formatDate(c.expirationDate)}</Grid>
                                                </>
                                            )
                                        }

                                        {
                                            c.requestStatus === 0 && (
                                                <>
                                                    <Grid item xs={3} style={{ fontWeight: "bold" }}>Lý do từ chối:</Grid>
                                                    <Grid item xs={9} color="red">{c.rejectionReason}</Grid>
                                                </>
                                            )
                                        }
                                    </Grid>
                                    <Box sx={{ display: "flex", gap: 2, mt: 2 }}>
                                        {
                                            c.certificateMedias.map((image) => {
                                                return (
                                                    <img style={{ width: "70px", height: "70px", cursor: "pointer" }}
                                                        onClick={() => { setCurrentImage(image.urlPath); setOpenViewImage(true) }}
                                                        src={image.urlPath}
                                                        key={image.id} />
                                                )
                                            })
                                        }
                                    </Box>
                                    {
                                        c.requestStatus === 2 && (<AccordionActions>
                                            <RejectCertificate certificateId={c.id} id={id} setCertificates={setCertificates}
                                                certificates={certificates} />
                                        </AccordionActions>)
                                    }
                                </AccordionDetails>
                            </Accordion>
                        )
                    })
                }
            </Paper >

            {
                currentImage && certificates && (
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
