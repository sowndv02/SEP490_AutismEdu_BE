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
function TutorWorkExperience() {
    const [open, setOpen] = useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);

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
                    <Typography variant='h3' textAlign="center">Kinh nghiệm</Typography>
                    <Box mt={3}>
                        <Accordion defaultExpanded>
                            <AccordionSummary
                                expandIcon={<ExpandMoreIcon />}
                                aria-controls="panel1-content"
                                id="panel1-header"
                            >
                                Công ty ABC XYZ
                            </AccordionSummary>
                            <AccordionDetails>
                                <Grid container columnSpacing={2} rowSpacing={3}>
                                    <Grid item xs={4}>Vị trí:</Grid>
                                    <Grid item xs={8}>Giáo viên</Grid>
                                    <Grid item xs={4}>Thời gian làm việc:</Grid>
                                    <Grid item xs={8}>09-20-2002 - 09-39-3030</Grid>
                                </Grid>
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
        </>
    )
}

export default TutorWorkExperience
