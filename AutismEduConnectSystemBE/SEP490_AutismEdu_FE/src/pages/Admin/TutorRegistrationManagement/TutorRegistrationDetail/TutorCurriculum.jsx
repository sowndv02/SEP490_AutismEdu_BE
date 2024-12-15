import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import { Accordion, AccordionActions, AccordionSummary, Box, Button, Paper, Stack, Typography } from '@mui/material';
import AccordionDetails from '@mui/material/AccordionDetails';
import RejectCurriculum from '../handleDialog/RejectCurriculum';
import FlagIcon from '@mui/icons-material/Flag';
function TutorCurriculum({ curriculums, id, setCurriculums }) {
    return (
        <Paper mt={3} sx={{ p: 2 }}>
            <Stack direction='row' mb={2} gap={2} bgcolor="#FFF9C4" p={1} borderRadius="5px"
                sx={{
                    border: "1px solid #FFEE58"
                }}
            >
                <FlagIcon sx={{ color: "#333333" }} />
                <Typography variant='h5' color="#333333">Khung chương trình</Typography>
            </Stack>
            {
                !curriculums || curriculums.length === 0 && (
                    <Typography>Không có khung chương trình</Typography>
                )
            }
            {
                curriculums && curriculums.map((c, index) => {
                    return (
                        <Accordion defaultExpanded={index === 0} key={c.id}>
                            <AccordionSummary
                                expandIcon={<ExpandMoreIcon />}
                                style={{ lineHeight: "20px" }}
                            ><span style={{ fontWeight: "bold", fontSize: "20px" }}>{c.ageFrom} - {c.ageEnd} tuổi</span> {
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
                                <Box sx={{ maxWidth: "100%" }} dangerouslySetInnerHTML={{ __html: c.description }} />
                                {
                                    c.requestStatus === 0 && (
                                        <>
                                            <Typography>Lý do từ chối: <span style={{ mt: 1, color: "red" }}>{c.rejectionReason}</span></Typography>
                                        </>
                                    )
                                }
                            </AccordionDetails>
                            {
                                c.requestStatus === 2 && (<AccordionActions>
                                    <RejectCurriculum curriculumId={c.id} curriculums={curriculums} setCurriculums={setCurriculums} />
                                </AccordionActions>)
                            }
                        </Accordion>
                    )
                })
            }
        </Paper>
    )
}

export default TutorCurriculum
