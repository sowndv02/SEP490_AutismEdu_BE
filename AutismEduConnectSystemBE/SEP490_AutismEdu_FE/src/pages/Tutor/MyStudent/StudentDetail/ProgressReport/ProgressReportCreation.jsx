import { Box, Button, FormControl, FormHelperText, List, ListItem, ListItemIcon, ListItemText, MenuItem, Modal, Select, Stack, TextField, Typography } from '@mui/material'
import React, { useEffect, useState } from 'react'
import MinimizeIcon from '@mui/icons-material/Minimize';
import ArrowRightIcon from '@mui/icons-material/ArrowRight';
import services from '~/plugins/services';
import { useFormik } from 'formik';
import LoadingComponent from '~/components/LoadingComponent';
import { enqueueSnackbar } from 'notistack';
import { Link, useParams } from 'react-router-dom';
import DoneIcon from '@mui/icons-material/Done';
import CloseIcon from '@mui/icons-material/Close';
import EditNoteIcon from '@mui/icons-material/EditNote';
import ListAltIcon from '@mui/icons-material/ListAlt';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import ConfirmDialog from '~/components/ConfirmDialog';
import PAGES from '~/utils/pages';
function ProgressReportCreation({ studentProfile, currentReport, setCurrentReport, setPreReport,
    progressReports, setProgressReports, currentPage, setSelectedItem, selectedItem, setSortType, setStartDate, setEndDate,
    startDate, endDate, setCurrentPage, sortType
}) {
    const [open, setOpen] = useState();
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);
    const [assessment, setAssessment] = useState([]);
    const [selectedAssessment, setSelectedAssessment] = useState([]);
    const [loading, setLoading] = useState(false);
    const { id } = useParams();
    const [openConfirm, setOpenConfirm] = useState(false);
    useEffect(() => {
        handleGetAsessment();
    }, [])
    useEffect(() => {
        if (!open) {
            formik.resetForm();
        } else {
            formik.resetForm({
                values: {
                    from: getFromDate(currentReport?.to) || '',
                    to: new Date().toISOString().split('T')[0],
                    achieved: '',
                    failed: '',
                    noteFromTutor: ''
                }
            })
        }
    }, [open])

    useEffect(() => {
        if (assessment && currentReport) {
            const preData = assessment.map((a) => {
                const choosenAss = currentReport.assessmentResults.find((r) => r.questionId === a.id);
                if (choosenAss) {
                    return {
                        questionId: choosenAss.questionId,
                        optionId: choosenAss.optionId
                    }
                } else {
                    return {
                        questionId: a.id,
                        optionId: a.assessmentOptions[0].id
                    }
                }
            });
            setSelectedAssessment(preData)
        }
    }, [assessment, currentReport])
    const handleGetAsessment = async () => {
        try {
            setLoading(true);
            await services.AssessmentManagementAPI.listAssessment((res) => {
                setAssessment(res.result.questions);
                const initialAssessment = res.result.questions.map((r) => {
                    return {
                        questionId: r.id,
                        optionId: r.assessmentOptions[0].id
                    }
                })
                setSelectedAssessment(initialAssessment)
            }, (err) => {
                console.log(err);
            })
            setLoading(false);
        } catch (error) {
            setLoading(false);
        }
    }

    const validate = (values) => {
        const errors = {};
        if (!values.achieved) {
            errors.achieved = "Bắt buộc";
        } else if (values.achieved.trim().length > 1000) {
            errors.achieved = "Chỉ chứa dưới 1000 ký tự"
        }
        if (!values.failed) {
            errors.failed = "Bắt buộc";
        } else if (values.failed.trim().length > 1000) {
            errors.failed = "Chỉ chứa dưới 1000 ký tự"
        }

        if (values.noteFromTutor.trim().length > 1000) {
            errors.noteFromTutor = "Chỉ chứa dưới 1000 ký tự"
        }
        if (!values.from) {
            errors.from = "Bắt buộc";
        }
        if (!values.to) {
            errors.to = "Bắt buộc";
        } else if (values.from >= values.to) {
            errors.from = "Ngày không hợp lệ"
        }
        return errors;
    }

    const getFromDate = () => {
        if (currentReport) {
            if (!currentReport.to) return "";
            const minDate = new Date(currentReport.to);
            minDate.setDate(minDate.getDate() + 1);
            const year = minDate.getFullYear();
            const month = String(minDate.getMonth() + 1).padStart(2, '0');
            const day = String(minDate.getDate()).padStart(2, '0');
            return `${year}-${month}-${day}`
        } else {
            if (!studentProfile.createdDate) return "";
            const minDate = new Date(studentProfile.createdDate);
            const year = minDate.getFullYear();
            const month = String(minDate.getMonth() + 1).padStart(2, '0');
            const day = String(minDate.getDate()).padStart(2, '0');
            return `${year}-${month}-${day}`
        }
    }
    const formik = useFormik({
        initialValues: {
            from: getFromDate(currentReport?.to) || '',
            to: new Date().toISOString().split('T')[0],
            achieved: '',
            failed: '',
            noteFromTutor: ''
        },
        validate,
        onSubmit: async (values) => {
            setOpenConfirm(true);
        }
    })
    const handleSubmit = async () => {
        try {
            setLoading(true);
            await services.ProgressReportAPI.createProgressReport({
                achieved: formik.values.achieved.trim(),
                failed: formik.values.failed.trim(),
                noteFromTutor: formik.values.noteFromTutor.trim(),
                from: formik.values.from,
                to: formik.values.to,
                studentProfileId: id,
                assessmentResults: selectedAssessment
            }, (res) => {
                enqueueSnackbar("Tạo sổ liên lạc thành công!", { variant: "success" })
                setCurrentReport(res.result);
                setPreReport(currentReport);
                if (currentPage !== 1 || startDate !== "" || endDate !== "" || sortType !== "desc") {
                    setStartDate("");
                    setEndDate("");
                    setSortType("desc");
                    setCurrentPage(1);
                } else if (currentPage === 1) {
                    let arr = [];
                    if (progressReports.length === 10) {
                        arr = [...progressReports.slice(0, 9)];
                    } else {
                        arr = [...progressReports]
                    }
                    setProgressReports([res.result, ...arr])
                    if (!selectedItem) {
                        setSelectedItem(res.result)
                    }
                }

                formik.resetForm();
                handleClose();
                setOpenConfirm(false);
            }, (err) => {
                enqueueSnackbar(err.error[0], { variant: "error" })
            })
            setLoading(false);
        } catch (error) {
            setLoading(false);
        }
    }
    const formatDate = (date) => {
        if (!date) {
            return "";
        }
        const d = new Date(date);
        return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`
    }
    return (
        <Box>
            <Button variant='contained' onClick={handleOpen}>Tạo đánh giá mới</Button>
            {
                open && <Modal
                    open={open}
                    onClose={handleClose}
                >
                    <Box sx={{
                        position: 'absolute',
                        top: '50%',
                        left: '50%',
                        transform: 'translate(-50%, -50%)',
                        width: currentReport ? "90vw" : '60vw',
                        bgcolor: 'background.paper',
                        boxShadow: 24,
                        p: 4,
                        maxHeight: "80vh",
                        overflow: 'auto'
                    }}>
                        <Stack direction='row' gap={2}>
                            <form onSubmit={formik.handleSubmit} style={{ width: currentReport ? "60%" : '100%' }}>
                                <Typography id="modal-modal-title" variant="h5" component="h2">
                                    Tạo đánh giá mới
                                </Typography>
                                <Stack direction='row' alignItems='center' gap={2} mt={3}>
                                    <Box>
                                        <Typography>Từ ngày</Typography>
                                        <TextField type='date' name='from'
                                            onChange={formik.handleChange}
                                            value={formik.values.from}
                                            inputProps={{
                                                min: getFromDate(currentReport?.to),
                                                max: new Date().toISOString().split('T')[0]
                                            }}
                                            disabled
                                        />
                                        {
                                            formik.errors.from && (
                                                <FormHelperText error>
                                                    {formik.errors.from}
                                                </FormHelperText>
                                            )
                                        }
                                    </Box>
                                    <MinimizeIcon />
                                    <Box>
                                        <Typography>Đến ngày</Typography>
                                        <TextField type='date' name='to'
                                            onChange={formik.handleChange}
                                            value={formik.values.to}
                                            inputProps={{
                                                min: getFromDate(currentReport?.to),
                                                max: new Date().toISOString().split('T')[0]
                                            }}
                                        />
                                        {
                                            formik.errors.to && (
                                                <FormHelperText error>
                                                    {formik.errors.to}
                                                </FormHelperText>
                                            )
                                        }
                                    </Box>
                                </Stack>

                                <Stack direction='row' gap={2} mt={2}>
                                    <DoneIcon sx={{ color: "green" }} />
                                    <Typography fontWeight="bold">Đã làm được</Typography>
                                </Stack>
                                <TextField multiline fullWidth minRows={5} maxRows={10}
                                    name='achieved'
                                    onChange={formik.handleChange}
                                    value={formik.values.achieved}
                                    sx={{ mt: 1 }}
                                />
                                <Stack direction='row' justifyContent='space-between'>
                                    <Box>
                                        {
                                            formik.errors.achieved && (
                                                <FormHelperText error>
                                                    {formik.errors.achieved}
                                                </FormHelperText>
                                            )
                                        }
                                    </Box>
                                    <Typography> {formik.values.achieved.length} / 1000</Typography>
                                </Stack>
                                <Stack direction='row' gap={2} mt={2}>
                                    <CloseIcon sx={{ color: "red" }} />
                                    <Typography fontWeight="bold">Chưa làm được</Typography>
                                </Stack>
                                <TextField multiline fullWidth minRows={5} maxRows={10}
                                    name='failed'
                                    onChange={formik.handleChange}
                                    value={formik.values.failed}
                                    sx={{ mt: 1 }} />
                                <Stack direction='row' justifyContent='space-between'>
                                    <Box>
                                        {
                                            formik.errors.failed && (
                                                <FormHelperText error>
                                                    {formik.errors.failed}
                                                </FormHelperText>
                                            )
                                        }
                                    </Box>
                                    <Typography> {formik.values.failed.length} / 1000</Typography>
                                </Stack>
                                <Stack direction='row' gap={2} mt={2}>
                                    <EditNoteIcon sx={{ color: "blue" }} />
                                    <Typography fontWeight="bold">Ghi chú thêm</Typography>
                                </Stack>
                                <TextField multiline fullWidth minRows={5} maxRows={10}
                                    name='noteFromTutor'
                                    onChange={formik.handleChange}
                                    value={formik.values.noteFromTutor}
                                    sx={{ mt: 1 }}
                                />
                                <Stack direction='row' justifyContent='space-between'>
                                    <Box>
                                        {
                                            formik.errors.noteFromTutor && (
                                                <FormHelperText error>
                                                    {formik.errors.noteFromTutor}
                                                </FormHelperText>
                                            )
                                        }
                                    </Box>
                                    <Typography> {formik.values.noteFromTutor.length} / 1000</Typography>
                                </Stack>
                                <Stack direction='row' gap={2} mt={2}>
                                    <ListAltIcon sx={{ color: "orange" }} />
                                    <Typography>Danh sách đánh giá</Typography>
                                </Stack>
                                <a href={PAGES.ASSESSMENT_GUILD} target="_blank"
                                    rel="noopener noreferrer">
                                    <Button variant='outlined' sx={{ mt: 2 }}>Xem cách thức đánh giá ?</Button>
                                </a>
                                <Stack direction='row' sx={{ width: "100%", mt: 3 }} flexWrap='wrap' rowGap={2}>
                                    {
                                        assessment && assessment.map((a, index) => {
                                            return (
                                                <Box sx={{ display: "flex", width: "50%" }} key={a.id}>
                                                    <ArrowRightIcon sx={{ fontSize: "40px", color: "red" }} />
                                                    <Box>
                                                        <Typography sx={{ width: "300px" }}>{a.question}</Typography>
                                                        <FormControl size='small' sx={{ width: "300px", mt: 1 }} key={a.id}>
                                                            <Select value={selectedAssessment[index].optionId}
                                                                onChange={(e) => {
                                                                    selectedAssessment[index].optionId = Number(e.target.value);
                                                                    setSelectedAssessment([...selectedAssessment]);
                                                                }}
                                                            >
                                                                {
                                                                    a.assessmentOptions.map((option) => {
                                                                        return (
                                                                            <MenuItem value={option.id} key={option.id}>{option.point} điểm</MenuItem>
                                                                        )
                                                                    })
                                                                }
                                                            </Select>
                                                        </FormControl>
                                                    </Box>
                                                </Box>
                                            )
                                        })
                                    }
                                </Stack>
                                <Box sx={{ display: "flex", gap: 1,justifyContent:"end" }}>
                                    <Button sx={{ mt: 5 }} onClick={handleClose} variant='outlined'>Huỷ</Button>
                                    <Button variant='contained' sx={{ mt: 5 }} type='submit'>Tạo sổ</Button>
                                </Box>
                            </form>
                            {
                                currentReport && (
                                    <Box sx={{ width: "38%" }}>
                                        <Typography variant='h5'>Đánh giá trước đó</Typography>
                                        <Typography mt={2}>Thời gian: {formatDate(currentReport?.from)} - {formatDate(currentReport?.to)}</Typography>
                                        <Stack direction='row' gap={2} mt={2}>
                                            <DoneIcon sx={{ color: "green" }} />
                                            <Typography>Đã làm được</Typography>
                                        </Stack>
                                        <Typography sx={{
                                            whiteSpace: "break-spaces", wordBreak: 'break-word',
                                            overflowWrap: 'break-word'
                                        }}>{currentReport.achieved}</Typography>
                                        <Stack direction='row' gap={2} mt={2}>
                                            <CloseIcon sx={{ color: "red" }} />
                                            <Typography>Chưa làm được</Typography>
                                        </Stack>
                                        <Typography sx={{
                                            whiteSpace: "break-spaces", wordBreak: 'break-word',
                                            overflowWrap: 'break-word'
                                        }}>{currentReport.failed}</Typography>
                                        <Stack direction='row' gap={2} mt={2}>
                                            <EditNoteIcon sx={{ color: "blue" }} />
                                            <Typography>Ghi chú thêm</Typography>
                                        </Stack>
                                        <Typography sx={{
                                            whiteSpace: "break-spaces", wordBreak: 'break-word',
                                            overflowWrap: 'break-word'
                                        }}>{currentReport.noteFromTutor}</Typography>
                                        <Stack direction='row' gap={2} mt={2}>
                                            <ListAltIcon sx={{ color: "orange" }} />
                                            <Typography>Danh sách đánh giá</Typography>
                                        </Stack>
                                        <List sx={{ height: '500px', overflow: "auto" }}>
                                            {
                                                currentReport && currentReport.assessmentResults.map((a) => {
                                                    return (
                                                        <ListItem key={a.id}>
                                                            <ListItemIcon>
                                                                <ChevronRightIcon />
                                                            </ListItemIcon>
                                                            <ListItemText
                                                                primary={a.question}
                                                                secondary={`Điểm: ${a.point}`}
                                                            />
                                                        </ListItem>
                                                    )
                                                })
                                            }
                                        </List>
                                    </Box>
                                )
                            }
                        </Stack>

                        <LoadingComponent open={loading} />
                        <ConfirmDialog openConfirm={openConfirm} setOpenConfirm={setOpenConfirm}
                            title={'Tạo sổ liên lạc'}
                            content={'Kiểm tra kỹ trước khi tạo! Bạn có muốn tạo sổ liên lạc này'}
                            handleAction={handleSubmit}
                        />
                    </Box>
                </Modal>
            }
        </Box >
    )
}

export default ProgressReportCreation
