import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import { Accordion, AccordionActions, AccordionSummary, Box, Grid, Paper, Stack, Typography } from '@mui/material';
import AccordionDetails from '@mui/material/AccordionDetails';
import WorkIcon from '@mui/icons-material/Work';
import RejectWorkExperiences from '../handleDialog/RejectWorkExperiences';
function TutorWorkExperience({ workExperiences, setWorkExperiences }) {
    const formatDate = (date) => {
        const dateObj = new Date(date);
        const formattedDate = dateObj.getDate().toString().padStart(2, '0') + '/' +
            (dateObj.getMonth() + 1).toString().padStart(2, '0') + '/' +
            dateObj.getFullYear();
        return formattedDate;
    }
    console.log(workExperiences);
    return (
        <Paper sx={{ p: 2, mt: 3 }}>
            <Stack direction='row' mb={2} gap={2} bgcolor="#FFF3E0" p={1} borderRadius="5px"
                sx={{
                    border: "1px solid #FFCC80"
                }}
            >
                <WorkIcon sx={{ color: "#6D4C41" }} />
                <Typography variant='h5' color="#6D4C41">Kinh nghiệm làm việc</Typography>
            </Stack>
            {
                workExperiences && workExperiences.map((w, index) => {
                    return (
                        <Accordion defaultExpanded={index === 0} key={w.id}
                            sx={{ lineHeight: "20px" }}>
                            <AccordionSummary
                                expandIcon={<ExpandMoreIcon />}
                            >
                                <span style={{ fontWeight: "bold", fontSize: "20px" }}>{w.companyName} </span>{
                                    w.requestStatus === 0 && <span style={{ color: "red", marginLeft: "20px" }}>(Đã từ chối)</span>
                                }
                                {
                                    w.requestStatus === 1 && <span style={{ color: "green", marginLeft: "20px" }}>(Đã chấp nhận)</span>
                                }
                                {
                                    w.requestStatus === 2 && <span style={{ color: "blue", marginLeft: "20px" }}>(Đang chờ)</span>
                                }
                            </AccordionSummary>
                            <AccordionDetails>
                                <Grid container columnSpacing={2} rowSpacing={3}>
                                    <Grid item xs={4} style={{ fontWeight: "bold" }}>Vị trí:</Grid>
                                    <Grid item xs={8}>{w.position}</Grid>
                                    <Grid item xs={4} style={{ fontWeight: "bold" }}>Thời gian làm việc:</Grid>
                                    <Grid item xs={8}>{formatDate(w.startDate)} - {w.endDate ? formatDate(w.endDate) : "Hiện tại"}</Grid>
                                    {
                                        w.requestStatus === 0 && (
                                            <>
                                                <Grid item xs={4} style={{ fontWeight: "bold" }}>Lý do từ chối:</Grid>
                                                <Grid item xs={8} color="red">{w.rejectionReason}</Grid>
                                            </>
                                        )
                                    }
                                </Grid>
                            </AccordionDetails>
                            {
                                w.requestStatus === 2 && (<AccordionActions>
                                    <RejectWorkExperiences workExperiencesId={w.id} workExperiences={workExperiences}
                                        setWorkExperiences={setWorkExperiences} />
                                </AccordionActions>)
                            }
                        </Accordion>
                    )
                })
            }
        </Paper>
    )
}

export default TutorWorkExperience
